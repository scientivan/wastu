using Wastu.Data;
using Wastu.Data.Repositories;
using Wastu.Domain.Catalog;
using Wastu.Domain.Climate;
using Wastu.Domain.Projects;

namespace Wastu.Services;

internal sealed class PostgreSqlBackend
{
    public PostgreSqlBackend(string connectionString)
    {
        var factory = new WastuDbContextFactory(connectionString);
        Repository = new ProyekRepositoryEf(factory);
        Iklim = new ClimateAnalysisService(Repository, new OpenMeteo());
        Proyek = new ProjectService(Repository, Iklim);
        Material = new MaterialService(new MaterialRepositoryEf(factory));
    }

    public ProyekRepositoryEf Repository { get; }
    public ProjectService Proyek { get; }
    public ClimateAnalysisService Iklim { get; }
    public MaterialService Material { get; }
}
