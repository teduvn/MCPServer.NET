using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Contracts;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Domain.Repositories;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace OrderManagement.Application.Common.Behaviors
{
    // Chỉ áp dụng cho command đã được đánh dấu transactional.
    public class AuditLogBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>, ITransactionalCommand
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false
        };

        private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "password",
            "token",
            "secret",
            "apikey",
            "authorization"
        };

        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AuditLogBehavior<TRequest, TResponse>> _logger;
        private readonly IMcpUserContextAccessor? _mcpUserContextAccessor;

        public AuditLogBehavior(
            ICurrentUserService currentUser,
            IAuditLogRepository auditLogRepository,
            IUnitOfWork unitOfWork,
            ILogger<AuditLogBehavior<TRequest, TResponse>> logger,
            IMcpUserContextAccessor? mcpUserContextAccessor = null)
        {
            _currentUser = currentUser;
            _auditLogRepository = auditLogRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mcpUserContextAccessor = mcpUserContextAccessor;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var auditLog = CreateAuditLog(request);

            try
            {
                var response = await next();
                stopwatch.Stop();

                auditLog.IsSuccess = true;
                auditLog.DurationMs = (int)stopwatch.ElapsedMilliseconds;

                await PersistAuditLogAsync(auditLog, cancellationToken);

                _logger.LogInformation(
                    "[Audit] {CommandType} by {UserId} ({ActorType}) succeeded in {DurationMs}ms",
                    auditLog.CommandType,
                    auditLog.UserId,
                    auditLog.ActorType,
                    auditLog.DurationMs);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                auditLog.IsSuccess = false;
                auditLog.ErrorMessage = ex.Message;
                auditLog.DurationMs = (int)stopwatch.ElapsedMilliseconds;

                await PersistAuditLogAsync(auditLog, CancellationToken.None);

                _logger.LogError(ex,
                    "[Audit] {CommandType} by {UserId} ({ActorType}) failed after {DurationMs}ms",
                    auditLog.CommandType,
                    auditLog.UserId,
                    auditLog.ActorType,
                    auditLog.DurationMs);

                throw;
            }
        }

        private AuditLog CreateAuditLog(TRequest request)
        {
            var auditLog = new AuditLog
            {
                UserId = _currentUser.UserId?.ToString() ?? "anonymous",
                UserName = _currentUser.Email ?? _currentUser.UserId?.ToString() ?? "anonymous",
                ActorType = _mcpUserContextAccessor?.User is null ? "human" : "ai-agent",
                CommandType = typeof(TRequest).Name,
                ToolName = typeof(TRequest).Name,
                Parameters = SanitizeAndSerialize(request),
                CorrelationId = Activity.Current?.TraceId.ToString() ?? string.Empty,
                IpAddress = string.Empty,
                CreatedAt = DateTime.UtcNow,
                EntityType = string.Empty,
                EntityId = string.Empty,
                ErrorMessage = string.Empty
            };

            PopulateEntityInfo(auditLog, request);
            return auditLog;
        }

        private async Task PersistAuditLogAsync(
            AuditLog auditLog,
            CancellationToken cancellationToken)
        {
            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static void PopulateEntityInfo(AuditLog auditLog, TRequest request)
        {
            var idProperty = typeof(TRequest)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(p => p.Name.EndsWith("Id", StringComparison.Ordinal)
                    && (p.PropertyType == typeof(Guid) || p.PropertyType == typeof(Guid?)));

            if (idProperty is null)
            {
                return;
            }

            auditLog.EntityId = idProperty.GetValue(request)?.ToString() ?? string.Empty;
            auditLog.EntityType = idProperty.Name[..^2];
        }

        private static string SanitizeAndSerialize(TRequest request)
        {
            try
            {
                var payload = typeof(TRequest)
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .ToDictionary(
                        property => property.Name,
                        property => SensitiveFields.Contains(property.Name)
                            ? "***REDACTED***"
                            : property.GetValue(request));

                return JsonSerializer.Serialize(payload, JsonOptions);
            }
            catch
            {
                return JsonSerializer.Serialize(request, JsonOptions);
            }
        }
    }
}
