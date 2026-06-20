namespace OrderManagement.Application.AuditLogs.DTOs
{
    public sealed class AuditLogDto
    {
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ActorType { get; set; } = string.Empty;
        public string CommandType { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public int DurationMs { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
