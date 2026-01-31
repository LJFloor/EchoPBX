using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace EchoPBX.Data.Helpers;

public static class CertificateHelper
{
    public static X509Certificate2 GenerateSelfSignedCertificate()
    {
        var distinguishedName = new X500DistinguishedName("CN=*");

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
}