using OrderManagement.Domain.Common;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.ValueObjects;

namespace OrderManagement.Domain.Entities
{
    public sealed class Customer : Entity
    {
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string PhoneNumber { get; private set; } = string.Empty;
        public Address? BillingAddress { get; private set; }
        public CustomerTier Tier { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public bool IsActive { get; private set; }

        private Customer() { }

        public static Result<Customer> Create(
            string firstName,
            string lastName,
            string email,
            string phoneNumber,
            Address? billingAddress = null)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                return CustomerErrors.FirstNameRequired;

            if (string.IsNullOrWhiteSpace(lastName))
                return CustomerErrors.LastNameRequired;

            if (string.IsNullOrWhiteSpace(email))
                return CustomerErrors.EmailRequired;

            if (!IsValidEmail(email))
                return CustomerErrors.InvalidEmail;

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                BillingAddress = billingAddress,
                Tier = CustomerTier.Standard,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            return customer;
        }

        public void UpdateContactInfo(string firstName, string lastName, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new DomainException("First name cannot be empty.");

            if (string.IsNullOrWhiteSpace(lastName))
                throw new DomainException("Last name cannot be empty.");

            FirstName = firstName;
            LastName = lastName;
            PhoneNumber = phoneNumber;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateBillingAddress(Address address)
        {
            if (address is null)
                throw new DomainException("Billing address cannot be null.");

            BillingAddress = address;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpgradeTier(CustomerTier newTier)
        {
            if (newTier <= Tier)
                throw new DomainException($"Cannot downgrade or set the same tier. Current: {Tier}, New: {newTier}");

            Tier = newTier;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            if (!IsActive)
                throw new DomainException("Customer is already inactive.");

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            if (IsActive)
                throw new DomainException("Customer is already active.");

            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public string GetFullName() => $"{FirstName} {LastName}";

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
