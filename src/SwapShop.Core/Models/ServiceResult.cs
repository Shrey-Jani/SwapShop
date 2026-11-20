namespace SwapShop.Core.Models;

public class ServiceResult
{
    private ServiceResult(bool succeeded, IEnumerable<string> errors)
    {
        Succeeded = succeeded;
        Errors = errors.ToList();
    }

    public bool Succeeded { get; }
    public IReadOnlyList<string> Errors { get; }

    public static ServiceResult Success() => new(true, []);

    public static ServiceResult Failure(params string[] errors) => new(false, errors);

    public static ServiceResult Failure(IEnumerable<string> errors) => new(false, errors);
}