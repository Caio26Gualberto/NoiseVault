namespace NoiseVault.Application.Response
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T Data { get; }
        public string Code { get; }
        public string Message { get; }
        public Error Error { get; }
        public bool IsFailure => !IsSuccess;

        public Result(T data, string code, string message)
        {
            IsSuccess = true;
            Data = data;
            Code = code;
            Message = message;
            Error = null!;
        }

        public Result(Error error)
        {
            IsSuccess = false;
            Error = error;
            Code = error.Code;
            Message = error.Message;
            Data = default!;
        }

        public static Result<T> Ok(T data, string code, string message) => new(data, code, message);
        public static Result<T> Fail(Error error) => new(error);
    }

    public class Result
    {
        public bool IsSuccess { get; }
        public string Code { get; }
        public string Message { get; }
        public Error Error { get; }
        public bool IsFailure => !IsSuccess;

        private Result(string code, string message)
        {
            IsSuccess = true;
            Code = code;
            Message = message;
            Error = null!;
        }

        private Result(Error error)
        {
            IsSuccess = false;
            Error = error;
            Code = error.Code;
            Message = error.Message;
        }

        public static Result Ok(string code, string message) => new(code, message);
        public static Result Fail(Error error) => new(error);
    }
}
