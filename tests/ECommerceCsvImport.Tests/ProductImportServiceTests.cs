using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;
using Xunit;

namespace ECommerceCsvImport.Tests;

public class ProductImportServiceTests
{
    private const string Header =
        "sku,name,description,price,currency,category_name,stock,image_url,is_active,brand,warranty_months,model_number";

    private const string Laptop =
        "1001,Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук для работы и учебы,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad3-15";

    private const string Asus =
        "1001,Ноутбук ASUS VivoBook,Другой ноутбук,52990,RUB,Компьютеры,7,/images/asus.jpg,true,ASUS,24,VivoBook";

    private static (ProductImportService service, InMemoryProductRepository repo) Create()
    {
        var repo = new InMemoryProductRepository();
        return (new ProductImportService(repo), repo);
    }

    [Fact]
    public void ImportFromLines_ValidRow_ImportsProduct()
    {
        var (service, repo) = Create();
        var result = service.ImportFromLines(new[] { Header, Laptop });

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Empty(result.Errors);

        var p = repo.GetBySku("1001");
        Assert.NotNull(p);
        Assert.Equal("Ноутбук Lenovo IdeaPad 3", p!.Name);
        Assert.Equal(45990m, p.Price);
        Assert.Equal("Lenovo", p.Brand);
        Assert.Equal(24, p.WarrantyMonths);
        Assert.Equal("IdeaPad3-15", p.ModelNumber);
        Assert.Equal("Бюджетный ноутбук для работы и учебы", p.Description);
        Assert.Equal("Компьютеры", p.CategoryName);
    }

    [Fact]
    public void ImportFromLines_TwoRows_ImportsBoth()
    {
        var (service, repo) = Create();
        var second = "1002,Мышь Logitech M185,Беспроводная мышь,1290,RUB,Периферия,10,/images/mouse.jpg,true,Logitech,12,M185";
        var result = service.ImportFromLines(new[] { Header, Laptop, second });

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.ImportedCount);
        Assert.Empty(result.Errors);
        Assert.Equal(2, repo.Count);
    }

    [Fact]
    public void ImportFromCsv_FileNotFound_ReturnsError()
    {
        var (service, _) = Create();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");
        var result = service.ImportFromCsv(path);

        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Файл не найден", result.Errors[0].Message);
    }

    [Fact]
    public void ImportFromCsv_EmptyFile_ReturnsError()
    {
        var (service, _) = Create();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");
        File.WriteAllText(path, string.Empty);
        try
        {
            var result = service.ImportFromCsv(path);
            Assert.Equal(0, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Данные для импорта отсутствуют", result.Errors[0].Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ImportFromLines_OnlyHeader_ReturnsNoDataError()
    {
        var (service, _) = Create();
        var result = service.ImportFromLines(new[] { Header });

        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Данные для импорта отсутствуют", result.Errors[0].Message);
    }

    [Fact]
    public void ImportFromLines_BadHeader_ReturnsError()
    {
        var (service, _) = Create();
        var result = service.ImportFromLines(new[]
        {
            "sku,name,description,price,currency,category_name,stock,image_url,is_active"
        });

        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Contains("Неверные заголовки CSV", result.Errors[0].Message);
    }

    [Fact]
    public void ImportFromLines_WrongFieldCount_ReturnsError()
    {
        var (service, _) = Create();
        var result = service.ImportFromLines(new[] { Header, "1001,Ноутбук Lenovo IdeaPad 3" });

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Contains("Неверное количество полей в строке", result.Errors[0].Message);
    }

    [Theory]
    [InlineData(",Ноутбук,Описание,45990,RUB,Компьютеры,5,/i.jpg,true,Lenovo,24,M1", "Поле sku не может быть пустым")]
    [InlineData("1001,,Описание,45990,RUB,Компьютеры,5,/i.jpg,true,Lenovo,24,M1", "Поле name не может быть пустым")]
    [InlineData("1003,Клавиатура,Описание,-500,RUB,Периферия,7,/i.jpg,true,A4Tech,24,V3M", "Поле price должно быть числом больше 0")]
    [InlineData("1003,Клавиатура,Описание,abc,RUB,Периферия,7,/i.jpg,true,A4Tech,24,V3M", "Поле price должно быть числом больше 0")]
    [InlineData("1004,Монитор,Описание,29990,RUB,Компьютеры,-5,/i.jpg,true,Dell,24,S27", "Остаток на складе должен быть целым числом не меньше 0")]
    [InlineData("1007,Webcam,Описание,4990,RUB,Периферия,6,/i.jpg,yes,Logitech,24,C920", "Поле is_active должно иметь значение true, false, 1 или 0")]
    [InlineData("1005,Видеокарта,Описание,24990,RUB,Компьютеры,4,/i.jpg,true,,36,RTX3060", "Поле brand обязательно для варианта «Электроника»")]
    [InlineData("1006,SSD,Описание,5990,RUB,Компьютеры,12,/i.jpg,true,Samsung,-1,870EVO", "Гарантия должна быть целым числом от 0 до 60 месяцев")]
    [InlineData("1006,SSD,Описание,5990,RUB,Компьютеры,12,/i.jpg,true,Samsung,61,870EVO", "Гарантия должна быть целым числом от 0 до 60 месяцев")]
    public void ImportFromLines_InvalidField_ReturnsExpectedError(string row, string expectedMessage)
    {
        var (service, repo) = Create();
        var result = service.ImportFromLines(new[] { Header, row });

        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, repo.Count);
        Assert.Contains(result.Errors, e => e.Message.Contains(expectedMessage));
    }

    [Fact]
    public void ImportFromLines_DuplicateInsideFile_SkipsSecond()
    {
        var (service, repo) = Create();
        var result = service.ImportFromLines(new[] { Header, Laptop, Asus });

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Артикул товара уже существует", result.Errors[0].Message);
        Assert.Equal("Ноутбук Lenovo IdeaPad 3", repo.GetBySku("1001")!.Name);
    }

    [Fact]
    public void ImportFromLines_DuplicateWithRepository_SkipsNew()
    {
        var (service, repo) = Create();
        repo.Add(new Product { Sku = "1001", Name = "Старый" });

        var result = service.ImportFromLines(new[] { Header, Asus });

        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, repo.Count);
        Assert.Contains("Артикул товара уже существует", result.Errors[0].Message);
    }

    [Fact]
    public void ImportFromLines_MultipleErrorsInOneRow_ReportsAll()
    {
        var (service, repo) = Create();
        var bad = ",,Некорректная строка,-10,RUB,Компьютеры,-5,/images/x.jpg,yes,,61,";
        var result = service.ImportFromLines(new[] { Header, bad });

        Assert.Equal(0, result.ImportedCount);
        Assert.True(result.ErrorCount >= 4);
        Assert.Equal(0, repo.Count);
    }

    [Fact]
    public void ImportFromLines_EmptyOptionalFields_Allowed()
    {
        var (service, repo) = Create();
        var row = "1011,Блок питания Corsair CV550,,4990,RUB,Компьютеры,9,,true,Corsair,60,CV550";
        var result = service.ImportFromLines(new[] { Header, row });

        Assert.Empty(result.Errors);
        var p = repo.GetBySku("1011")!;
        Assert.Equal(string.Empty, p.Description);
        Assert.Equal(string.Empty, p.ImageUrl);
    }

    [Fact]
    public void ImportFromLines_EmptyCurrency_BecomesRub()
    {
        var (service, repo) = Create();
        var row = "1011,Блок питания,,4990,,Компьютеры,9,,true,Corsair,60,CV550";
        service.ImportFromLines(new[] { Header, row });
        Assert.Equal("RUB", repo.GetBySku("1011")!.Currency);
    }

    [Fact]
    public void ImportFromLines_EmptyCategory_BecomesDefault()
    {
        var (service, repo) = Create();
        var row = "1011,Блок питания,,4990,RUB,,9,,true,Corsair,60,CV550";
        service.ImportFromLines(new[] { Header, row });
        Assert.Equal("Без категории", repo.GetBySku("1011")!.CategoryName);
    }
}