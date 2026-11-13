using ECommerceCsvImport.Services;

namespace ECommerceCsvImport.Web.Infrastructure;

public static class SampleDataSeeder
{
    private const string SampleFileRelativePath = "csv/sample_products.csv";

    public static void Seed(IServiceProvider services, string webRootPath)
    {
        var repository = services.GetRequiredService<IProductRepository>();
        if (repository.Count > 0)
            return;

        var path = Path.Combine(webRootPath, SampleFileRelativePath);
        if (!File.Exists(path))
            return;

        services.GetRequiredService<IProductImportService>().ImportFromCsv(path);
    }
}
