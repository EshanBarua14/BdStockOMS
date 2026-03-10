namespace BdStockOMS.API.Models;

/// <summary>
/// Configurable session policy per role.
/// Stored in SystemSettings as JSON or as individual keys.
/// This model is used in-memory by SessionPolicyService.
/// </summary>
public class SessionPolicy
{
    public string RoleName              { get; set; } = string.Empty;
    public int MaxConcurrentSessions    { get; set; } = 1;   // 0 = unlimited
    public int InactivityTimeoutMinutes { get; set; } = 30;  // 0 = never
    public bool MfaRequired             { get; set; } = false;
    public bool SingleSessionOnly       { get; set; } = false; // force logout others on new login
}
