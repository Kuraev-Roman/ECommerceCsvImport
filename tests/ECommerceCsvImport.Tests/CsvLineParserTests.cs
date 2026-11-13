using ECommerceCsvImport.Services;
using Xunit;

namespace ECommerceCsvImport.Tests;

public class CsvLineParserTests
{
    [Fact]
    public void Parse_SimpleLine_Returns12Fields()
    {
        var f = CsvLineParser.Parse("1001,Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук для работы и учебы,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad3-15");
        Assert.Equal(12, f.Count);
        Assert.Equal("1001", f[0]);
    }

    [Fact]
    public void Parse_EmptyFields_AreEmpty()
    {
        var f = CsvLineParser.Parse("1011,Блок питания Corsair CV550,,4990,RUB,Компьютеры,9,,true,Corsair,60,CV550");
        Assert.Equal(12, f.Count);
        Assert.Equal("", f[2]);
        Assert.Equal("", f[7]);
    }

    [Fact]
    public void Parse_CommaInsideQuotes_StaysInOneField()
    {
        var f = CsvLineParser.Parse("1012,\"Ноутбук, игровой\",Тестовая строка,59990,RUB,Компьютеры,1,/images/notebook.jpg,true,ASUS,36,VivoBook");
        Assert.Equal(12, f.Count);
        Assert.Equal("Ноутбук, игровой", f[1]);
    }

    [Fact]
    public void Parse_EscapedQuotes_AreUnescaped()
    {
        var f = CsvLineParser.Parse("1013,\"Название \"\"Special\"\"\",Описание,100,RUB,Категория,5,/img.jpg,true,Brand,12,Model");
        Assert.Equal("Название \"Special\"", f[1]);
    }
}