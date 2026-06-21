namespace FCG.Payments.Domain.Common;

public class Result<T>
{
    public T? Value { get; }
    public IReadOnlyList<string> Errors { get; }
    public bool IsSuccess => !Errors.Any();

    private Result(T value)
    {
        Value = value;
        Errors = [];
    }

    private Result(IEnumerable<string> errors)
    {
        Errors = errors.ToList().AsReadOnly();
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(IEnumerable<string> errors) => new(errors);
    public static Result<T> Failure(string error) => new([error]);
}
