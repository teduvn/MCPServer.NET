using OrderManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.ValueObjects
{
    public sealed record Address
    {
        public string Street { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string Province { get; init; } = string.Empty;
        public string Country { get; init; } = string.Empty;
        public string? PostalCode { get; init; }

        // Constructor để tương thích với code hiện tại
        public Address(string street, string city, string province, string country, string? postalCode = null)
        {
            Street = street;
            City = city;
            Province = province;
            Country = country;
            PostalCode = postalCode;
        }

        // Parameterless constructor cho EF Core
        private Address() { }

        public static Address Create(
            string street, string city, string province,
            string country, string? postalCode = null)
        {
            if (string.IsNullOrWhiteSpace(street))
                throw new DomainException("Địa chỉ đường không được để trống.");

            if (string.IsNullOrWhiteSpace(city))
                throw new DomainException("Thành phố không được để trống.");

            if (string.IsNullOrWhiteSpace(country) || country.Length != 2)
                throw new DomainException("Country code phải là 2 ký tự (VD: VN, US).");

            return new Address(
                street.Trim(), city.Trim(),
                province.Trim(), country.ToUpperInvariant().Trim(),
                postalCode?.Trim());
        }

        // Value Object có thể có behaviour
        public bool IsDomestic() => Country == "VN";

        public string ToFormattedString() =>
            PostalCode is null
                ? $"{Street}, {City}, {Province}, {Country}"
                : $"{Street}, {City}, {Province} {PostalCode}, {Country}";

        public override string ToString() => ToFormattedString();
    }

}
