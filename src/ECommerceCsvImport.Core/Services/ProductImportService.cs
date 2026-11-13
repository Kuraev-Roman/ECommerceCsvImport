using System.Globalization;
using ECommerceCsvImport.Models;

namespace ECommerceCsvImport.Services;


public class ProductImportService : IProductImportService
{
    private const string DefaultCurrency = "RUB";
    private const string DefaultCategory = "Без категории";
    private const int MinWarrantyMonths = 0;
    private const int MaxWarrantyMonths = 60;

    private static readonly string[] RequiredColumns =
    {
        "sku", "name", "description", "price", "currency", "category_name",
        "stock", "image_url", "is_active", "brand", "warranty_months", "model_number"
    };

    private readonly IProductRepository _repository;

    public ProductImportService(IProductRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }


    public ImportResult ImportFromCsv(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return FileError("Файл не найден");

        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath);
        }
        catch (IOException)
        {
            return FileError("Не удалось прочитать файл");
        }
        catch (UnauthorizedAccessException)
        {
            return FileError("Нет доступа к файлу");
        }

        return ImportFromLines(lines);
    }

  
    public ImportResult ImportFromLines(IEnumerable<string> lines)
    {
        var result = new ImportResult();
        var allLines = lines?.ToList() ?? new List<string>();

        int headerIndex = allLines.FindIndex(l => !string.IsNullOrWhiteSpace(l));
        if (headerIndex < 0)
        {
            AddError(result, 0, "Данные для импорта отсутствуют");
            return result;
        }

   
        var header = CsvLineParser.Parse(allLines[headerIndex].TrimStart('\uFEFF'));
        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Count; i++)
        {
            string columnName = header[i].Trim();
            if (!columnMap.ContainsKey(columnName))
                columnMap[columnName] = i;
        }

        if (RequiredColumns.Any(c => !columnMap.ContainsKey(c)))
        {
            AddError(result, headerIndex + 1, "Неверные заголовки CSV");
            return result;
        }

        int expectedFields = header.Count;
        var importedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

   
        for (int index = headerIndex + 1; index < allLines.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(allLines[index]))
                continue;

            result.TotalRows++;
            int rowNumber = index + 1;
            var values = CsvLineParser.Parse(allLines[index]);

            if (values.Count != expectedFields)
            {
                AddError(result, rowNumber, "Неверное количество полей в строке");
                continue;
            }

            string GetValue(string column) =>
                columnMap.TryGetValue(column, out int colIndex) && colIndex < values.Count
                    ? values[colIndex]
                    : string.Empty;

            var rowErrors = new List<ImportError>();

            string sku = GetValue("sku").Trim();
            if (string.IsNullOrWhiteSpace(sku))
                rowErrors.Add(Error(rowNumber, "Поле sku не может быть пустым"));

            string name = GetValue("name").Trim();
            if (string.IsNullOrWhiteSpace(name))
                rowErrors.Add(Error(rowNumber, "Поле name не может быть пустым"));

            if (!(TryParseDecimal(GetValue("price"), out decimal price) && price > 0))
                rowErrors.Add(Error(rowNumber, "Поле price должно быть числом больше 0"));

            if (!(TryParseInt(GetValue("stock"), out int stock) && stock >= 0))
                rowErrors.Add(Error(rowNumber, "Остаток на складе должен быть целым числом не меньше 0"));

            if (!TryParseBool(GetValue("is_active"), out bool isActive))
                rowErrors.Add(Error(rowNumber, "Поле is_active должно иметь значение true, false, 1 или 0"));

            string brand = GetValue("brand").Trim();
            if (string.IsNullOrWhiteSpace(brand))
                rowErrors.Add(Error(rowNumber, "Поле brand обязательно для варианта «Электроника»"));

            if (!(TryParseInt(GetValue("warranty_months"), out int warranty)
                  && warranty >= MinWarrantyMonths && warranty <= MaxWarrantyMonths))
                rowErrors.Add(Error(rowNumber, "Гарантия должна быть целым числом от 0 до 60 месяцев"));

            if (rowErrors.Count > 0)
            {
                result.Errors.AddRange(rowErrors);
                continue;
            }

          
            if (_repository.GetBySku(sku) != null || importedSkus.Contains(sku))
            {
                AddError(result, rowNumber, "Артикул товара уже существует");
                continue;
            }

            var product = new Product
            {
                Sku = sku,
                Name = name,
                Description = GetValue("description").Trim(),
                Price = price,
                Currency = NormalizeCurrency(GetValue("currency")),
                CategoryName = NormalizeCategory(GetValue("category_name")),
                StockQuantity = stock,
                ImageUrl = GetValue("image_url").Trim(),
                IsActive = isActive,
                Brand = brand,
                WarrantyMonths = warranty,
                ModelNumber = GetValue("model_number").Trim()
            };

            _repository.Add(product);
            importedSkus.Add(sku);
            result.ImportedCount++;
        }

        if (result.TotalRows == 0 && result.Errors.Count == 0)
            AddError(result, 0, "Данные для импорта отсутствуют");

        return result;
    }



    private static ImportResult FileError(string message)
    {
        var result = new ImportResult();
        AddError(result, 0, message);
        return result;
    }

    private static void AddError(ImportResult result, int row, string message) =>
        result.Errors.Add(Error(row, message));

    private static ImportError Error(int row, string message) =>
        new ImportError { RowNumber = row, Message = message };

    private static bool TryParseDecimal(string value, out decimal result) =>
        decimal.TryParse(value.Trim().Replace(',', '.'),
            NumberStyles.Number, CultureInfo.InvariantCulture, out result);

    private static bool TryParseInt(string value, out int result) =>
        int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    private static bool TryParseBool(string value, out bool result)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "true":
            case "1":
                result = true;
                return true;
            case "false":
            case "0":
                result = false;
                return true;
            default:
                result = false;
                return false;
        }
    }

    private static string NormalizeCurrency(string value) =>
        string.IsNullOrWhiteSpace(value) ? DefaultCurrency : value.Trim().ToUpperInvariant();

    private static string NormalizeCategory(string value) =>
        string.IsNullOrWhiteSpace(value) ? DefaultCategory : value.Trim();
}