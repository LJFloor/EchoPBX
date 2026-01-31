using System.Text.Json;
using System.Text.Json.Serialization;
using EchoPBX.Data.Dto;

namespace EchoPBX.Web.Converters;

public class UploadedFileJsonConverter : JsonConverter<UploadedFile>
{
    public override UploadedFile? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return string.IsNullOrWhiteSpace(str)
            ? null
            : UploadedFile.FromDataUrl(str);
    }

    public override void Write(Utf8JsonWriter writer, UploadedFile value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}