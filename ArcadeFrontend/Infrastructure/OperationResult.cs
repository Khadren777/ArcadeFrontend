using System;

namespace ArcadeFrontend.Infrastructure
{
    public enum FailureCategory
    {
        None,
        Validation,
        Configuration,
        MissingFile,
        MissingDirectory,
        LaunchFailure,
        ProcessFailure,
        Unauthorized,
        Unexpected
    }

    public class OperationResult
    {
        public bool IsSuccess { get; protected init; }
        public FailureCategory FailureCategory { get; protected init; } = FailureCategory.None;
        public string UserMessage { get; protected init; } = string.Empty;
        public string? TechnicalMessage { get; protected init; }
        public Exception? Exception { get; protected init; }

        public static OperationResult Success(string userMessage = "Operation completed successfully.")
        {
            return new OperationResult { IsSuccess = true, UserMessage = userMessage };
        }

        public static OperationResult Fail(string userMessage, FailureCategory failureCategory, string? technicalMessage = null, Exception? exception = null)
        {
            return new OperationResult
            {
                IsSuccess = false,
                FailureCategory = failureCategory,
                UserMessage = string.IsNullOrWhiteSpace(userMessage) ? "The operation failed." : userMessage,
                TechnicalMessage = technicalMessage,
                Exception = exception
            };
        }
    }

    public class OperationResult<T> : OperationResult
    {
        public T? Data { get; private init; }

        public static OperationResult<T> Success(T data, string userMessage = "Operation completed successfully.")
        {
            return new OperationResult<T> { IsSuccess = true, UserMessage = userMessage, Data = data };
        }

        public static new OperationResult<T> Fail(string userMessage, FailureCategory failureCategory, string? technicalMessage = null, Exception? exception = null)
        {
            return new OperationResult<T>
            {
                IsSuccess = false,
                FailureCategory = failureCategory,
                UserMessage = string.IsNullOrWhiteSpace(userMessage) ? "The operation failed." : userMessage,
                TechnicalMessage = technicalMessage,
                Exception = exception
            };
        }
    }
}
