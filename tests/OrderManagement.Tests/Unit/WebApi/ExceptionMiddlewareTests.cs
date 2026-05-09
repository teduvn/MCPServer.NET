using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OrderManagement.Application.Common.Exceptions;
using OrderManagement.Domain.Entities;
using OrderManagement.WebAPI.Middleware;
using System.Text.Json;

namespace OrderManagement.Tests.Unit.WebApi
{
    public class ExceptionMiddlewareTests
    {
        private static HttpContext CreateHttpContext(string path = "/api/test")
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Response.Body = new MemoryStream();
            return context;
        }

        [Fact]
        public async Task NotFoundException_Returns404WithProblemDetails()
        {
            // Arrange
            var logger = Substitute.For<ILogger<ExceptionMiddleware>>();
            var env = Substitute.For<IHostEnvironment>();
            env.EnvironmentName.Returns(Environments.Production);

            RequestDelegate next = _ => throw new NotFoundException(nameof(Order), Guid.NewGuid());

            var middleware = new ExceptionMiddleware(next, logger, env);
            var context = CreateHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(404);
            context.Response.ContentType.Should().Contain("application/problem+json");

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var pd = JsonSerializer.Deserialize<ProblemDetails>(body);

            pd!.Status.Should().Be(404);
            pd.Title.Should().Be("Resource Not Found");
        }

        [Fact]
        public async Task UnhandledException_Returns500_WithoutLeakingDetail()
        {
            // Arrange
            var logger = Substitute.For<ILogger<ExceptionMiddleware>>();
            var env = Substitute.For<IHostEnvironment>();
            env.EnvironmentName.Returns(Environments.Production);

            var secretMessage = "ConnectionString: Server=prod-db;Password=supersecret";
            RequestDelegate next = _ => throw new InvalidOperationException(secretMessage);

            var middleware = new ExceptionMiddleware(next, logger, env);
            var context = CreateHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert — 500 nhưng KHÔNG chứa secret message
            context.Response.StatusCode.Should().Be(500);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

            body.Should().NotContain(secretMessage);
            body.Should().NotContain("ConnectionString");
            body.Should().NotContain("supersecret");
        }

        [Fact]
        public async Task ValidationException_Returns422_WithErrorDictionary()
        {
            // Arrange
            var logger = Substitute.For<ILogger<ExceptionMiddleware>>();
            var env = Substitute.For<IHostEnvironment>();
            env.EnvironmentName.Returns(Environments.Production);

            var failures = new[]
            {
                new ValidationFailure("CustomerId", "CustomerId không được để trống"),
                new ValidationFailure("Items", "Phải có ít nhất 1 sản phẩm")
            };
            RequestDelegate next = _ => throw new ValidationException(failures);

            var middleware = new ExceptionMiddleware(next, logger, env);
            var context = CreateHttpContext("/api/orders");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(422);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var vpd = JsonSerializer.Deserialize<ValidationProblemDetails>(body);

            vpd!.Errors.Should().ContainKey("CustomerId");
            vpd.Errors.Should().ContainKey("Items");
        }
    }

}
