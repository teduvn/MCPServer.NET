using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OrderManagement.Application.Contracts;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OrderManagement.Infrastructure.Auth
{
    public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
    {
        public string GenerateToken(Guid userId, string email, IEnumerable<string> roles)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = new SymmetricSecurityKey(
                                  Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));

            // Claim là các mảnh thông tin được encode vào token
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub,   userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()), // unique token id
            };

            // Thêm role claims — mỗi role là 1 claim riêng
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                                        int.Parse(jwtSettings["ExpiryMinutes"]!)),
                signingCredentials: new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

}
