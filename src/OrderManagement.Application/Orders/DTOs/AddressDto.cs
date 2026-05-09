namespace OrderManagement.Application.Orders.DTOs
{
    /// <summary>
    /// DTO cho Address Value Object.
    /// </summary>
    public sealed record AddressDto
    {
        public string Street { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string Province { get; init; } = string.Empty;
        public string Country { get; init; } = string.Empty;
        public string? PostalCode { get; init; }
        public string FormattedAddress { get; init; } = string.Empty;
    }
}
