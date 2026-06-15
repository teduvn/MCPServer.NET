namespace OrderManagement.McpServer.Authorization
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class,
     AllowMultiple = true)]
    public class RequiresRoleAttribute : Attribute
    {
        public string[] Roles { get; }


        public RequiresRoleAttribute(params string[] roles)
        {
            Roles = roles;
        }
    }

}
