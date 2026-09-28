using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace EchoPBX.Data.Helpers;

public static class CertificateHelper
{
    /// <summary>
    /// Directory holding the certificate for the HTTPS port.
    /// </summary>
    public static readonly string CertificateDirectory = Path.Combine(Constants.DataDirectory, "certs");

    /// <summary>
    /// Load the certificate for the HTTPS port. A cert.pem and key.pem placed in the certificate
    /// directory take precedence. Without them, a self-signed certificate is created once and
    /// reused until it is about to expire.
    /// </summary>
    public static X509Certificate2 LoadOrCreate()
    {
        var certPath = Path.Combine(CertificateDirectory, "cert.pem");
        var keyPath = Path.Combine(CertificateDirectory, "key.pem");
        if (File.Exists(certPath) && File.Exists(keyPath))
        {
            return X509Certificate2.CreateFromPemFile(certPath, keyPath);
        }

        var selfSignedPath = Path.Combine(CertificateDirectory, "self-signed.pfx");
        if (File.Exists(selfSignedPath))
        {
            var existing = new X509Certificate2(selfSignedPath, (string?)null, X509KeyStorageFlags.Exportable);
            if (existing.NotAfter > DateTime.Now.AddDays(30))
            {
                return existing;
            }
        }

        var certificate = GenerateSelfSignedCertificate();
        Directory.CreateDirectory(CertificateDirectory);
        File.WriteAllBytes(selfSignedPath, certificate.Export(X509ContentType.Pfx));
        return certificate;
    }

    public static X509Certificate2 GenerateSelfSignedCertificate()
    {
        var distinguishedName = new X500DistinguishedName("CN=EchoPBX");

        using var rsa = RSA.Create(2048);

        var request = new CertificateRequest(
            distinguishedName,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Add extensions
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                false));

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, // Server Authentication
                false));

        // Browsers ignore the CN and only look at the SAN, so list every name the server is reachable by
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost");
        san.AddDnsName(Environment.MachineName);
        foreach (var address in GetLocalAddresses())
        {
            san.AddIpAddress(address);
        }

        request.CertificateExtensions.Add(san.Build());

        // Create the certificate (valid for 1 year)
        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(1));

        // Export and re-import to make the private key exportable (required for PFX)
        return new X509Certificate2(
            certificate.Export(X509ContentType.Pfx),
            (string?)null,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
    }

    private static IEnumerable<System.Net.IPAddress> GetLocalAddresses()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(x => x.OperationalStatus == OperationalStatus.Up)
            .SelectMany(x => x.GetIPProperties().UnicastAddresses)
            .Select(x => x.Address)
            .Where(x => x.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6 && !x.IsIPv6LinkLocal)
            .Distinct();
    }
}
