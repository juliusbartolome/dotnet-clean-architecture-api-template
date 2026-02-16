using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using LaunchpadStarter.IntegrationTests.Common;

namespace LaunchpadStarter.IntegrationTests;

public sealed class CatalogEndpointsTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly IConfiguration _configuration = factory.Services.GetRequiredService<IConfiguration>();

    [Fact]
    public async Task CreateProduct_HappyPath_ShouldReturnCreated()
    {
        var token = GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var payload = new
        {
            sku = "SKU_HAPPY",
            name = "Happy Path",
            description = "desc",
            price = 22.30m,
            currency = "USD"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/products", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateProduct_InvalidPayload_ShouldReturnValidationProblem()
    {
        var token = GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var payload = new
        {
            sku = "bad sku",
            name = "",
            description = "desc",
            price = -1m,
            currency = "US"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/products", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("validation.failed");
    }

    [Fact]
    public async Task GetProductById_SecondCall_ShouldReturnCacheHitHeader()
    {
        var token = GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createPayload = new
        {
            sku = "SKU_CACHE",
            name = "Cache Product",
            description = "cache",
            price = 11.00m,
            currency = "USD"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", createPayload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedProductResponse>();
        created.Should().NotBeNull();

        _client.DefaultRequestHeaders.Authorization = null;

        var firstResponse = await _client.GetAsync($"/api/v1/products/{created!.id}");
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstResponse.Headers.GetValues("X-Cache").Single().Should().Be("MISS");

        var secondResponse = await _client.GetAsync($"/api/v1/products/{created.id}");
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.Headers.GetValues("X-Cache").Single().Should().Be("HIT");
    }

    private string GenerateToken()
    {
        var issuer = _configuration["Jwt:Issuer"] ?? "LaunchpadStarter.Api";
        var audience = _configuration["Jwt:Audience"] ?? "LaunchpadStarter.Client";
        var signingKey = _configuration["Jwt:SigningKey"] ?? "replace-with-long-dev-key-please-change";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: [new Claim("scope", "catalog.write")],
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed record CreatedProductResponse(Guid id);
}
