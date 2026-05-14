namespace PointsTracker.Domain.Enums;

public enum GlobalRole
{
    User,
    Admin,
    SuperAdmin
}

public static class GlobalRoleExtensions
{
    public static string ToWireString(this GlobalRole role) => role switch
    {
        GlobalRole.User => "user",
        GlobalRole.Admin => "admin",
        GlobalRole.SuperAdmin => "super_admin",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };

    public static GlobalRole ParseGlobalRole(string value) => value switch
    {
        "user" => GlobalRole.User,
        "admin" => GlobalRole.Admin,
        "super_admin" => GlobalRole.SuperAdmin,
        _ => throw new ArgumentException($"Unknown global role '{value}'", nameof(value))
    };

    public static bool TryParseGlobalRole(string? value, out GlobalRole role)
    {
        switch (value)
        {
            case "user": role = GlobalRole.User; return true;
            case "admin": role = GlobalRole.Admin; return true;
            case "super_admin": role = GlobalRole.SuperAdmin; return true;
            default: role = GlobalRole.User; return false;
        }
    }

    public static bool IsAtLeast(this GlobalRole actual, GlobalRole required) =>
        (int)actual >= (int)required;
}
