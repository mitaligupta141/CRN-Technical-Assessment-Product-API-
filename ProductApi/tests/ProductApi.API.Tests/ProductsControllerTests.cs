using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProductApi.Application.DTOs;
using Xunit;

namespace ProductApi.API.Tests;

public class ProductsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }  
}
