namespace OrderManagement.Domain.Common
{
    public abstract class Entity
    {
        public Guid Id { get; protected set; } = default!;

        protected Entity() { }

        protected Entity(Guid id)
        {
            Id = id;
        }

        private readonly List<IDomainEvent> _domainEvents = new();

        // Chỉ expose IReadOnlyCollection — bên ngoài không thể modify trực tiếp
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();


        // Equality dựa trên ID, không phải reference
        public override bool Equals(object? obj)
        {
            if (obj is not Entity other) return false;
            if (ReferenceEquals(this, other)) return true;
            if (GetType() != other.GetType()) return false;
            return Id.Equals(other.Id);
        }

        public static bool operator ==(Entity? left, Entity? right)
            => left?.Equals(right) ?? right is null;

        public static bool operator !=(Entity? left, Entity? right)
            => !(left == right);

        public override int GetHashCode()
            => HashCode.Combine(GetType(), Id);

        // Protected — chỉ aggregate root tự raise event của mình
        protected void RaiseDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        // Infrastructure gọi sau khi SaveChanges thành công
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

    }

}
