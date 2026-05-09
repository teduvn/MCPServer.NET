using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Customers
{
    public static class CustomerErrors
    {
        public static readonly Error NotFound =
            Error.Create("Customer.NotFound", "Customer was not found.");

        public static readonly Error FirstNameRequired =
            Error.Create("Customer.FirstNameRequired", "First name is required.");

        public static readonly Error LastNameRequired =
            Error.Create("Customer.LastNameRequired", "Last name is required.");

        public static readonly Error EmailRequired =
            Error.Create("Customer.EmailRequired", "Email is required.");

        public static readonly Error InvalidEmail =
            Error.Create("Customer.InvalidEmail", "Invalid email format.");

        public static readonly Error AlreadyInactive =
            Error.Create("Customer.AlreadyInactive", "Customer is already inactive.");

        public static readonly Error AlreadyActive =
            Error.Create("Customer.AlreadyActive", "Customer is already active.");

        public static Error CustomerNotFound(Guid customerId) =>
            Error.Create("Customer.NotFound", $"Customer {customerId} does not exist.");

        public static Error InvalidTierUpgrade(string currentTier, string newTier) =>
            Error.Create("Customer.InvalidTierUpgrade", $"Cannot downgrade or set the same tier. Current: {currentTier}, New: {newTier}");
    }
}
