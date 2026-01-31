namespace EchoPBX.Data.Helpers;

public abstract class FfmpegHelper
{
    /// <summary>
    /// Converts audio bytes to WAV format using FFmpeg and saves to the specified path.
    /// </summary>
    /// <param name="bytes">The input audio bytes.</param>
    /// <param name="savePath">The path to save the converted WAV file.</param>
    public static async Task SaveAsWav(byte[] bytes, string savePath)
    {
        var tempInputPath = Path.GetTempFileName();
        await File.WriteAllBytesAsync(tempInputPath, bytes);

        var tempOutputPath = Path.GetTempFileName();
        var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = "ffmpeg";
        process.StartInfo.Arguments = $"-i \"{tempInputPath}\" -map 0:a -map_metadata -1 -ar 8000 -ac 1 -f wav \"{tempOutputPath}\" -y";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            File.Delete(tempInputPath);
            File.Delete(tempOutputPath);
            throw new Exception($"FFmpeg failed with exit code {process.ExitCode}: {error}");
        }

        var outputBytes = await File.ReadAllBytesAsync(tempOutputPath);
        File.Delete(tempInputPath);
        File.Delete(tempOutputPath);

        var directory = Path.GetDirectoryName(savePath);
        Directory.CreateDirectory(directory!);
        await File.WriteAllBytesAsync(savePath, outputBytes);
    }
}