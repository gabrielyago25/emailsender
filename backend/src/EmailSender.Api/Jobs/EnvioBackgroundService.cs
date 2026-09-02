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
            _logger.LogWarning($"Envio - {request.JobId} não foi encontrado.");

            return;
        }

        job.Status = StatusOperacaoEnvio.EmAndamento;
        job.IniciadoEm = DateTime.UtcNow;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var envioService = scope.ServiceProvider.GetRequiredService<EnvioService>();
            var progresso = new JobProgress(job);
            var resultado = await envioService.EnviarAsync(request.Destinatarios, request.Assunto, request.Corpo, progresso, stoppingToken);

            job.Processados = resultado.Total;
            job.Enviados = resultado.Enviados;
            job.Falhas = resultado.Falhas;
            job.DetalhesFalhas = resultado.DetalhesFalhas;

            job.SegundosRestantes = null;
            job.EtapaAtual = null;

            job.Status = StatusOperacaoEnvio.Concluido;
            job.FinalizadoEm = DateTime.UtcNow;
        } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            job.Status = StatusOperacaoEnvio.Cancelado;
            job.FinalizadoEm = DateTime.UtcNow;

            _logger.LogInformation($"Envio - {job.Id} interrompido durante o encerramento da aplicação.");
        } catch (Exception ex)
        {
            job.Status = StatusOperacaoEnvio.Falhou;
            job.Erro = ex.Message;
            job.FinalizadoEm = DateTime.UtcNow;

            _logger.LogError(ex, $"Erro inesperado ao processar {job.Id}.");
        }
    }

    private sealed class JobProgress : IProgress<ProgressoEnvio>
    {
        private readonly EnvioJob _job;
        public JobProgress(EnvioJob job)
        {
            _job = job;
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
            }
        }
    }
}