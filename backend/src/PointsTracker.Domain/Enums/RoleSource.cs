namespace PointsTracker.Domain.Enums;

public enum RoleSource
{
    Default,
    IdpClaim,
    ManualOverride
}

public static class RoleSourceExtensions
{
    public static string ToWireString(this RoleSource source) => source switch
    {
        RoleSource.Default => "default",
        RoleSource.IdpClaim => "idp_claim",
        RoleSource.ManualOverride => "manual_override",
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
    };

    public static RoleSource ParseRoleSource(string value) => value switch
    {
        "default" => RoleSource.Default,
        "idp_claim" => RoleSource.IdpClaim,
        "manual_override" => RoleSource.ManualOverride,
        _ => throw new ArgumentException($"Unknown role source '{value}'", nameof(value))
    };
}
