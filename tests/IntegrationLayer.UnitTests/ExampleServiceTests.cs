using IntegrationLayer.Core.Models;
using IntegrationLayer.ExampleService.Repositories;
using IntegrationLayer.ExampleService.Services;
using NSubstitute;
using ExampleServiceImpl = IntegrationLayer.ExampleService.Services.ExampleService;

namespace IntegrationLayer.UnitTests;

public class ExampleServiceTests
{
    private readonly IExampleRepository _repository = Substitute.For<IExampleRepository>();
    private readonly ExampleServiceImpl _sut;

    public ExampleServiceTests()
    {
        _sut = new ExampleServiceImpl(_repository);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllItems()
    {
        var expected = new[] { new ExampleModel { Id = 1, Name = "Test" } };
        _repository.GetAllAsync().Returns(expected);

        var result = await _sut.GetAllAsync();

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsItem_WhenFound()
    {
        var expected = new ExampleModel { Id = 1, Name = "Test" };
        _repository.GetByIdAsync(1).Returns(expected);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _repository.GetByIdAsync(99).Returns((ExampleModel?)null);

        var result = await _sut.GetByIdAsync(99);

        Assert.Null(result);
    }
}
