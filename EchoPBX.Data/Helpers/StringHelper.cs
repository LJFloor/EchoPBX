using System.Security.Cryptography;

namespace EchoPBX.Data.Helpers;

public static class StringHelper
{
    /// <summary>
    /// Cleans a phone number by removing all non-digit characters.
    /// </summary>
    /// <param name="phoneNumber">The phone number to clean.</param>
    /// <returns>The cleaned phone number containing only digits.</returns>
    public static string CleanPhoneNumber(string phoneNumber)
    {
        return new string(phoneNumber.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    /// Generates a random alphanumeric string of the specified length.
    /// </summary>
    /// <param name="length">The length of the string to generate.</param>
    /// <returns>The generated random string.</returns>
    public static string GenerateRandomString(int length)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        const string chars = "BCDFGHJKLMNPQRSTVWXYZbcdfgijklmnpqrstvwxyz0123456789";
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    /// <summary>
    /// In goes the path to a sound file on disk, out comes the URL path to that sound file.
    /// </summary>
    /// <example>/data/sounds/trunk-1/dtmf-announcement => /sounds/trunk-1/dtmf-accouncement.wav</example>
    public static string BuildSoundUrl(string path)
    {
        var result = path.Replace(Constants.DataDirectory, "");
        result = result.Replace("\\", "/");
        if (!result.EndsWith(".wav"))
        {
            result += ".wav";
        }
        
        return result;
    }
}