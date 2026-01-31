namespace EchoPBX.Web.Authentication;

/// <summary>
/// Signifies that access to the decorated class or method requires administrative privileges.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireAdminAttribute : Attribute
{
}