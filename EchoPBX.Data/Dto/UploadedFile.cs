namespace EchoPBX.Data.Dto;

public class UploadedFile
{
    public byte[] Content { get; set; } = null!;
    
    public string MimeType { get; set; } = null!;

    public static UploadedFile FromDataUrl(string url)
    {
        var parts = url.Split(',', 2);
        if (parts.Length != 2)
        {
            throw new ArgumentException("Invalid data URL format.");
        }

        var meta = parts[0];
        var data = parts[1];

        var mimeType = "application/octet-stream"; // Default MIME type
        if (meta.StartsWith("data:") && meta.Contains(";base64"))
        {
            var mimePart = meta.Substring(5, meta.IndexOf(";base64") - 5);
            if (!string.IsNullOrEmpty(mimePart))
            {
                mimeType = mimePart;
            }
        }

        var content = Convert.FromBase64String(data);

        return new UploadedFile
        {
            Content = content,
            MimeType = mimeType
        };
    }
}