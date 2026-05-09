using MediatR;
using OrderManagement.Application.Contracts;
using OrderManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Auth.Command.Login
{
    public sealed class LoginCommandHandler(
     IIdentityService identityService,
     IJwtTokenService jwtTokenService) // Đã định nghĩa ở bài 5.3
     : IRequestHandler<LoginCommand, Result<LoginResponse>>
    {
        public async Task<Result<LoginResponse>> Handle(
            LoginCommand request,
            CancellationToken ct)
        {
            // Bước 1: Validate credentials qua Identity
            var credResult = await identityService.ValidateCredentialsAsync(
                request.Email, request.Password, ct);

            if (credResult.IsFailure)
                return Result<LoginResponse>.Failure(credResult.Error);

            var userId = credResult.Value;

            // Bước 2: Lấy roles từ Identity
            var roles = await identityService.GetUserRolesAsync(userId);

            // Bước 3: Tạo JWT token với thông tin từ Identity
            var token = jwtTokenService.GenerateToken(userId, request.Email, roles);

            return Result<LoginResponse>.Success(new LoginResponse(
                UserId: userId,
                Email: request.Email,
                AccessToken: token,
                Roles: roles));
        }
    }

}
