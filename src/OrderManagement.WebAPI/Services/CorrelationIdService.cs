using OrderManagement.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.WebAPI.Services
{
    public class CorrelationIdService : ICorrelationIdService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;


        public CorrelationIdService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }


        public string CorrelationId =>
            _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString()
            ?? "no-context"; // Background job, test, hoặc non-HTTP context
    }

}
