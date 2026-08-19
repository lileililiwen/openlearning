namespace OpenLearning.Jobs.Services;

/// <summary>Resolves an <see cref="IJob"/> implementation by its key.</summary>
public class JobResolver
{
    private readonly IEnumerable<IJob> _jobs;

    public JobResolver(IEnumerable<IJob> jobs)
    {
        _jobs = jobs;
    }

    public IJob? Resolve(string key)
    {
        return _jobs.FirstOrDefault(j => j.Key == key);
    }

    public IReadOnlyList<IJob> All()
    {
        return _jobs.ToList();
    }
}
