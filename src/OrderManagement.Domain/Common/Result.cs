namespace OrderManagement.Domain.Common
{
    public class Result
    {
        // Protected constructor — chỉ tạo qua factory method
        protected Result(bool isSuccess, Error error)
        {
            // Không cho phép trạng thái mâu thuẫn
            if (isSuccess && error != Error.None)
                throw new InvalidOperationException();
            if (!isSuccess && error == Error.None)
                throw new InvalidOperationException();

            IsSuccess = isSuccess;
            Error = error;
        }

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public Error Error { get; }

        // Factory methods
        public static Result Success() => new(true, Error.None);
        public static Result Failure(Error error) => new(false, error);

        // Implicit conversion — tự động wrap
        public static implicit operator Result(Error error) => Failure(error);
    }

    public sealed class Result<TValue> : Result
    {
        private readonly TValue? _value;

        private Result(TValue value) : base(true, Error.None)
        {
            _value = value;
        }

        private Result(Error error) : base(false, error)
        {
            _value = default;
        }

        // Value chỉ lấy được khi IsSuccess == true
        public TValue Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("Failure result has no value.");

        public static Result<TValue> Success(TValue value) => new(value);
        public static new Result<TValue> Failure(Error error) => new(error);

        // Tự động wrap value thành Result<TValue>
        public static implicit operator Result<TValue>(TValue value) => Success(value);
        public static implicit operator Result<TValue>(Error error) => Failure(error);
    }


}
