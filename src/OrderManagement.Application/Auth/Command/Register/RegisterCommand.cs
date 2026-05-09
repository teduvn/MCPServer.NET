using MediatR;
using OrderManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Auth.Command.Register
{
    public sealed record RegisterCommand(
     string FullName,
     string Email,
     string Password) : IRequest<Result<Guid>>;

}
