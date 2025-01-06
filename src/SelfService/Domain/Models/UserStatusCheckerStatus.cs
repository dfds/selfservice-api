namespace SelfService.Domain.Models;

public class UserStatusCheckerStatus : ValueObject
{
    public static readonly UserStatusCheckerStatus Deactivated = new("Deactivated");
    public static readonly UserStatusCheckerStatus Found = new("Found");
    public static readonly UserStatusCheckerStatus NotFound = new("NotFound");
    public static readonly UserStatusCheckerStatus NoAuthToken = new("NoAuthToken");
    public static readonly UserStatusCheckerStatus BadAuthToken = new("BadAuthToken");
    public static readonly UserStatusCheckerStatus Unknown = new("Unknown");

    private readonly string _value;

    private static readonly Dictionary<string, UserStatusCheckerStatus> UserStatusMap = new()
    {
        { Deactivated.ToString(), Deactivated },
        { Found.ToString(), Found },
        { NotFound.ToString(), NotFound },
        { NoAuthToken.ToString(), NoAuthToken },
        { BadAuthToken.ToString(), BadAuthToken },
        { Unknown.ToString(), Unknown },
    };

    private UserStatusCheckerStatus(string status)
    {
        _value = status;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    public override string ToString()
    {
        return _value;
    }

    public static bool TryParse(string? text, out UserStatusCheckerStatus role)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            role = null!;
            return false;
        }

        bool success = UserStatusMap.TryGetValue(text, out var mappedRole);
        role = success ? mappedRole! : null!;
        return success;
    }
}
