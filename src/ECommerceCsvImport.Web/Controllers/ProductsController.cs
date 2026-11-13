using System.Text;
using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceCsvImport.Web.Controllers;

public class ProductsController : Controller
{
    private const long MaxUploadSizeBytes = 5 * 1024 * 1024;

    private readonly IProductRepository _repository;
    private readonly IProductImportService _importService;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(
        IProductRepository repository,
        IProductImportService importService,
        IWebHostEnvironment environment)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    [HttpGet]
    public ActionResult Index()
    {
        var viewModel = new ProductsIndexViewModel { Products = _repository.GetAll() };
        return View(viewModel);
    }

    [HttpGet]
    public ActionResult Import()
    {
        return View(new ImportViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Import(IFormFile? file, string? filePath)
    {
        ImportResult result;

        if (file != null && file.Length > 0)
        {
            result = await ImportUploadedFileAsync(file);
        }
        else
        {
            var resolvedPath = ResolveFilePath(filePath);
            result = string.IsNullOrWhiteSpace(resolvedPath)
                ? CreateErrorResult("Выберите файл или укажите путь к CSV-файлу")
                : _importService.ImportFromCsv(resolvedPath);
        }

        return View("Import", new ImportViewModel
        {
            FilePath = filePath ?? string.Empty,
            Result = result
        });
    }

    private async Task<ImportResult> ImportUploadedFileAsync(IFormFile file)
    {
        if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            return CreateErrorResult("Выберите файл с расширением .csv");

        if (file.Length > MaxUploadSizeBytes)
            return CreateErrorResult("Размер файла превышает 5 МБ");

        var lines = new List<string>();
        try
        {
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
                lines.Add(line);
        }
        catch (IOException)
        {
            return CreateErrorResult("Не удалось прочитать файл");
        }

        return _importService.ImportFromLines(lines);
    }

    private static ImportResult CreateErrorResult(string message)
    {
        var result = new ImportResult();
        result.Errors.Add(new ImportError { RowNumber = 0, Message = message });
        return result;
    }

    private string? ResolveFilePath(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var trimmed = input.Trim();

        if (Path.IsPathRooted(trimmed) && !trimmed.StartsWith("/") && !trimmed.StartsWith("\\"))
            return trimmed;

        trimmed = trimmed.TrimStart('/', '\\');
        return Path.Combine(_environment.WebRootPath, trimmed);
    }
}
