using EmailSender.Core.Models;
using EmailSender.Core.Services;

namespace EmailSender.Api.Jobs;

public class EnvioBackgroundService : BackgroundService
{
    private readonly EnvioJobQueue _queue;
    private readonly EnvioJobStore _store;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EnvioBackgroundService> _logger;

    public EnvioBackgroundService(EnvioJobQueue queue, EnvioJobStore store, IServiceScopeFactory scopeFactory, ILogger<EnvioBackgroundService> logger)
    {
        _queue = queue;
        _store = store;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            EnvioJobRequest request;
            try
            {
                request = await _queue.ObterProximoAsync(stoppingToken);
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessarJobAsync(request, stoppingToken);
        }
    }
    private async Task ProcessarJobAsync(EnvioJobRequest request, CancellationToken stoppingToken)
    {
        var job = _store.Obter(request.JobId);

        if (job is null)
        {
            _logger.LogWarning("Envio {JobId} não foi encontrado.", request.JobId);

            return;
        }

        job.Status = StatusOperacaoEnvio.EmAndamento;
        job.IniciadoEm = DateTime.UtcNow;
        var statusFinal = StatusOperacaoEnvio.Falhou;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var envioService = scope.ServiceProvider.GetRequiredService<EnvioService>();
            var progresso = new JobProgress(job, _logger);
            var resultado = await envioService.EnviarAsync(request.Destinatarios, request.Assunto, request.Corpo, progresso, stoppingToken);

            job.Processados = resultado.Total;
            job.Enviados = resultado.Enviados;
            job.Falhas = resultado.Falhas;
            job.DetalhesFalhas = resultado.DetalhesFalhas;

            statusFinal = StatusOperacaoEnvio.Concluido;
        } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            statusFinal = StatusOperacaoEnvio.Cancelado;
            _logger.LogInformation("Envio {JobId} interrompido durante o encerramento da aplicação.", job.Id);
        } catch (Exception ex)
        {
            job.Erro = "O processamento foi interrompido por uma falha interna. Consulte o responsável pelo sistema antes de reenviar.";

            // Não registrar a mensagem ou o objeto da exceção, que podem conter dados sensíveis.
            _logger.LogError("Erro inesperado ao processar {JobId}. Tipo: {TipoFalha}.", job.Id, ex.GetType().Name);
        }
        finally
        {
            job.EtapaAtual = null;
            job.SegundosRestantes = null;
            job.DestinatarioAtual = null;
            job.FinalizadoEm = DateTime.UtcNow;
            // O frontend encerra o polling ao observar um estado final.
            job.Status = statusFinal;
        }
    }

    private sealed class JobProgress : IProgress<ProgressoEnvio>
    {
        private readonly EnvioJob _job;
        private readonly ILogger<EnvioBackgroundService> _logger;
        public JobProgress(EnvioJob job, ILogger<EnvioBackgroundService> logger)
        {
            _job = job;
            _logger = logger;
        }

        public void Report(ProgressoEnvio value)
        {
            _job.Processados = value.Processados;
            _job.EtapaAtual = value.Status;
            _job.DestinatarioAtual = value.Email;
            _job.SegundosRestantes = value.Status == StatusEnvio.Aguardando ? value.SegundosRestantes : null;

            if (value.Status == StatusEnvio.Enviado)
            {
                _job.Enviados++;
            }
            if (value.Status == StatusEnvio.Falha)
            {
                _job.Falhas++;
                _logger.LogWarning("Falha no envio {JobId}, destinatário de ordem {Ordem}. Tipo: {TipoFalha}.",
                    _job.Id, value.Processados, value.TipoFalha);
            }
        }
    }
}
