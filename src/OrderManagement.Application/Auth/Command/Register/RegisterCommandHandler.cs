using MediatR;
using OrderManagement.Application.Contracts;
using OrderManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Auth.Command.Register
{
    public sealed class RegisterCommandHandler(
     IIdentityService identityService) // Inject interface — không biết UserManager
     : IRequestHandler<RegisterCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(
            RegisterCommand request,
            CancellationToken ct)
        {
            return await identityService.CreateUserAsync(
                request.Email,
                request.FullName,
                request.Password,
                ct);
        }
    }

}
