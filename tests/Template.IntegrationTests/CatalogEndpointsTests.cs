using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Template.IntegrationTests.Common;

namespace Template.IntegrationTests;

public sealed class CatalogEndpointsTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateProduct_HappyPath_ShouldReturnCreated()
    {
        var token = GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

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
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

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

    private static string GenerateToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("integration-tests-signing-key-at-least-32-characters"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "Template.Api",
            audience: "Template.Client",
            claims: [new Claim("scope", "catalog.write")],
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed record CreatedProductResponse(Guid id);
}
