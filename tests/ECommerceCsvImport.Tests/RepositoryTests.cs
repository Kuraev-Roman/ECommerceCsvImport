using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;
using Xunit;

namespace ECommerceCsvImport.Tests;

public class RepositoryTests
{
    private static Product Make(string sku = "1001") =>
        new Product { Sku = sku, Name = "Ноутбук Lenovo IdeaPad 3", Price = 45990m, Brand = "Lenovo" };

    [Fact]
    public void Add_AddsProduct_GetAllContainsIt()
    {
        var repo = new InMemoryProductRepository();
        repo.Add(Make());

        var all = repo.GetAll();
        Assert.Single(all);
        Assert.Equal("1001", all[0].Sku);
        Assert.Equal(1, repo.Count);
    }

    [Fact]
    public void GetBySku_ReturnsProduct()
    {
        var repo = new InMemoryProductRepository();
        repo.Add(Make());
        Assert.NotNull(repo.GetBySku("1001"));
    }

    [Fact]
    public void GetBySku_IgnoresSpacesAndCase()
    {
        var repo = new InMemoryProductRepository();
        repo.Add(Make("ABC1"));
        Assert.NotNull(repo.GetBySku(" abc1 "));
    }

    [Fact]
    public void EmptyRepository_IsEmpty()
    {
        var repo = new InMemoryProductRepository();
        Assert.Equal(0, repo.Count);
        Assert.Empty(repo.GetAll());
        Assert.Null(repo.GetBySku("1001"));
    }
}