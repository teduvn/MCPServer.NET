using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Contracts
{
    public interface IJwtTokenService
    {
        string GenerateToken(Guid userId, string email, IEnumerable<string> roles);
    }

}
