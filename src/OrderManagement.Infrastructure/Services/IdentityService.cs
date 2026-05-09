using Microsoft.AspNetCore.Identity;
using OrderManagement.Application.Contracts;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Domain.Repositories;
using OrderManagement.Infrastructure.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Services
{
    public sealed class IdentityService(
     UserManager<AppUser> userManager,
     IUnitOfWork unitOfWork,    // Thay ApplicationDbContext bằng IUnitOfWork
     IUserRepository userRepository // Thêm để tạo Domain User qua repository
    ) : IIdentityService
    {
        public async Task<Result<Guid>> CreateUserAsync(
            string email,
            string fullName,
            string password,
            CancellationToken ct = default)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null)
                return Result<Guid>.Failure(new Error("Identity.DuplicateEmail", "Email đã được sử dụng."));

            var userId = Guid.NewGuid();
            var appUser = new AppUser
            {
                Id = userId,
                UserName = email,
                Email = email,
                FullName = fullName,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            await unitOfWork.BeginTransactionAsync(ct);
            try
            {
                // Ghi AppUser qua UserManager — Identity xử lý hash password, stamp
                var result = await userManager.CreateAsync(appUser, password);
                if (!result.Succeeded)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Result<Guid>.Failure(new Error("Identity.CreateFailed", errors));
                }

                // Ghi Domain User qua repository — không gọi context trực tiếp
                var domainUser = User.Create(userId, fullName, email);
                await userRepository.AddAsync(domainUser, ct);

                // CommitTransactionAsync tự gọi SaveChangesAsync bên trong
                await unitOfWork.CommitTransactionAsync(ct);
                return Result<Guid>.Success(userId);
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }

        public async Task<Result<Guid>> ValidateCredentialsAsync(
            string email,
            string password,
            CancellationToken ct = default)
        {
            var appUser = await userManager.FindByEmailAsync(email);
            if (appUser is null)
                return Result<Guid>.Failure(new Error("Identity.InvalidCredentials", "Email hoặc mật khẩu không đúng."));

            var isValid = await userManager.CheckPasswordAsync(appUser, password);
            if (!isValid)
                return Result<Guid>.Failure(new Error("Identity.InvalidCredentials", "Email hoặc mật khẩu không đúng."));

            return Result<Guid>.Success(appUser.Id);
        }

        public async Task<IReadOnlyList<string>> GetUserRolesAsync(Guid userId)
        {
            var appUser = await userManager.FindByIdAsync(userId.ToString());
            if (appUser is null) return [];

            var roles = await userManager.GetRolesAsync(appUser);
            return roles.ToList().AsReadOnly();
        }

        public async Task<Result> AssignRoleAsync(
            Guid userId, string role, CancellationToken ct = default)
        {
            var appUser = await userManager.FindByIdAsync(userId.ToString());
            if (appUser is null)
                return Result.Failure(new Error("Identity.UserNotFound", "User không tồn tại."));

            var result = await userManager.AddToRoleAsync(appUser, role);
            return result.Succeeded
                ? Result.Success()
                : Result.Failure(new Error("Identity.RoleAssignmentFailed", string.Join(", ", result.Errors.Select(e => e.Description))));
        }
    }

}
