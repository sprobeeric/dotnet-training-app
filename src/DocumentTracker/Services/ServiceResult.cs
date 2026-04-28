namespace DocumentTracker.Services;

public class ServiceResult
{
    private readonly List<ServiceError> _errors = [];

    public bool Succeeded => _errors.Count == 0;
    public IReadOnlyList<ServiceError> Errors => _errors;

    public static ServiceResult Success() => new();

    public static ServiceResult Failure(string key, string message)
    {
        var result = new ServiceResult();
        result.AddError(key, message);
        return result;
    }

    public void AddError(string key, string message) => _errors.Add(new ServiceError(key, message));
}

public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; private set; }

    public static ServiceResult<T> Success(T value) => new() { Value = value };

    public new static ServiceResult<T> Failure(string key, string message)
    {
        var result = new ServiceResult<T>();
        result.AddError(key, message);
        return result;
    }
}
