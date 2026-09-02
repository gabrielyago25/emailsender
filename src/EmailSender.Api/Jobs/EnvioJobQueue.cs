using System.Threading.Channels;

namespace EmailSender.Api.Jobs;

public class EnvioJobQueue
{
    private readonly Channel<EnvioJobRequest> _queue = Channel.CreateUnbounded<EnvioJobRequest>();
    public async ValueTask EnfileirarAsync(EnvioJobRequest request, CancellationToken cancellationToken = default)
    {
        await _queue.Writer.WriteAsync(request, cancellationToken);
    }
    public async ValueTask<EnvioJobRequest> ObterProximoAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}