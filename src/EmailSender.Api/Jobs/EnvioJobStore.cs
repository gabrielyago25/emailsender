using System.Collections.Concurrent;

namespace EmailSender.Api.Jobs;

public class EnvioJobStore
{
    private readonly ConcurrentDictionary<Guid, EnvioJob> _jobs = new();
    public EnvioJob Criar(int total)
    {
        var job = new EnvioJob
        {
            Total = total
        };
        _jobs[job.Id] = job;
        return job;
    }
    public EnvioJob? Obter(Guid id)
    {
        _jobs.TryGetValue(id, out var job);
        return job;
    }
}