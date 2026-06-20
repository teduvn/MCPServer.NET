namespace OrderManagement.McpServer.Authorization
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class RequiresClaimAttribute : Attribute
    {
        public RequiresClaimAttribute(string claimType, params string[] values)
        {
            ClaimType = claimType;
            Values = values ?? [];
        }

        public string ClaimType { get; }

        public string[] Values { get; }
    }
}
