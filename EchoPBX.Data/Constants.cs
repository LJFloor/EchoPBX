using System.Reflection;

namespace EchoPBX.Data;

/// <summary>
/// Constants used throughout the EchoPBX application.
/// </summary>
public static class Constants
{
    private static readonly Version DataVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);
    
    /// <summary>
    /// The HTTP port EchoPBX is served on.
    /// </summary>
    public const ushort HttpPort = 8740;

    /// <summary>
    /// The HTTPS port EchoPBX is served on. The webphone needs it, since browsers only allow
    /// microphone access on secure pages.
    /// </summary>
    public const ushort HttpsPort = 8741;

    /// <summary>
    /// The current version of EchoPBX
    /// </summary>
    public static readonly string Version = $"{DataVersion.Major}.{DataVersion.Minor}.{DataVersion.Build}";

    /// <summary>
    /// Directory where EchoPBX data is stored.
    /// </summary>
#if DEBUG
    public static readonly string DataDirectory = $"/home/{Environment.UserName}/echopbx-data";
#else
    // inside the docker container
    public const string DataDirectory = "/data";
#endif
}