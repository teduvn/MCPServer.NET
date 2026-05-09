using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace OrderManagement.Tests.Integration
{
    // tests/OrderManagement.Tests/Integration/ErrorHandlingTests.cs
    public class ErrorHandlingTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ErrorHandlingTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetOrder_WithInvalidId_Returns404ProblemDetails()
        {
            // Act
            var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Content.Headers.ContentType!.MediaType
                .Should().Be("application/problem+json");

            var pd = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            pd!.Status.Should().Be(404);
            pd.Title.Should().Be("Resource Not Found");
        }
    }

}
