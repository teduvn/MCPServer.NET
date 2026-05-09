using MediatR;
using OrderManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Auth.Command.Login
{
    public sealed record LoginCommand(
    string Email,
    string Password) : IRequest<Result<LoginResponse>>;
    public sealed record LoginResponse(
    Guid UserId,
    string Email,
    string AccessToken,
    IReadOnlyList<string> Roles);

}
