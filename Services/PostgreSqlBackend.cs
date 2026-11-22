using Wastu.Data;
using Wastu.Data.Repositories;
using Wastu.Domain.Catalog;
using Wastu.Domain.Climate;
using Wastu.Domain.Emissions;
using Wastu.Domain.Projects;
using Wastu.Domain.Scoring;
using Wastu.Domain.Weather;
using Wastu.Reporting;

namespace Wastu.Services;

internal sealed class PostgreSqlBackend
{
    public PostgreSqlBackend(string connectionString)
    {
        var factory = new WastuDbContextFactory(connectionString);
        ProyekRepository = new ProyekRepositoryEf(factory);
        MaterialRepository = new MaterialRepositoryEf(factory);
        SumberBahanRepository = new SumberBahanRepositoryEf(factory);
        BatchEvaluasiRepository = new BatchEvaluasiRepositoryEf(factory);
        EvaluasiRepository = new EvaluasiRepositoryEf(factory);
        PekerjaanRepository = new PekerjaanRepositoryEf(factory);
        PrakiraanCacheRepository = new PrakiraanCacheRepositoryEf(factory);
        AmbangPekerjaanRepository = new AmbangPekerjaanRepositoryEf(factory);
        KonfigurasiModelRepository = new KonfigurasiModelRepositoryEf(factory);
        LaporanRepository = new LaporanRepositoryEf(factory);

        OpenMeteoClient = new OpenMeteo();
        Iklim = new ClimateAnalysisService(ProyekRepository, OpenMeteoClient);
        Proyek = new ProjectService(ProyekRepository, Iklim);
        Material = new MaterialService(MaterialRepository);

        SuitabilityScoring = new SuitabilityScoringService(
            MaterialRepository,
            ProyekRepository,
            BatchEvaluasiRepository,
            EvaluasiRepository);

        EmissionSimulation = new EmissionSimulationService(
            EvaluasiRepository,
            MaterialRepository);

        WorkScheduling = new WorkSchedulingService(
            PekerjaanRepository,
            ProyekRepository,
            PrakiraanCacheRepository,
            OpenMeteoClient,
            AmbangPekerjaanRepository);

        ReportExport = new ReportExportService(
            LaporanRepository,
            ProyekRepository,
            EvaluasiRepository,
            MaterialRepository,
            SumberBahanRepository,
            PekerjaanRepository,
            KonfigurasiModelRepository);
    }

    public ProyekRepositoryEf Repository => ProyekRepository;
    public ProyekRepositoryEf ProyekRepository { get; }
    public MaterialRepositoryEf MaterialRepository { get; }
    public SumberBahanRepositoryEf SumberBahanRepository { get; }
    public BatchEvaluasiRepositoryEf BatchEvaluasiRepository { get; }
    public EvaluasiRepositoryEf EvaluasiRepository { get; }
    public PekerjaanRepositoryEf PekerjaanRepository { get; }
    public PrakiraanCacheRepositoryEf PrakiraanCacheRepository { get; }
    public AmbangPekerjaanRepositoryEf AmbangPekerjaanRepository { get; }
    public KonfigurasiModelRepositoryEf KonfigurasiModelRepository { get; }
    public LaporanRepositoryEf LaporanRepository { get; }

    public OpenMeteo OpenMeteoClient { get; }
    public ProjectService Proyek { get; }
    public ClimateAnalysisService Iklim { get; }
    public MaterialService Material { get; }
    public SuitabilityScoringService SuitabilityScoring { get; }
    public EmissionSimulationService EmissionSimulation { get; }
    public WorkSchedulingService WorkScheduling { get; }
    public ReportExportService ReportExport { get; }
}
