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
        var tempOutputPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempInputPath, bytes);

            using var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "ffmpeg";
            process.StartInfo.Arguments = $"-nostdin -i \"{tempInputPath}\" -map 0:a -map_metadata -1 -ar 8000 -ac 1 -f wav \"{tempOutputPath}\" -y";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();

            // ffmpeg writes a lot to stderr. Read it while it runs, or it blocks once the pipe is full.
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            await output;

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg failed with exit code {process.ExitCode}: {await error}");
            }

            var outputBytes = await File.ReadAllBytesAsync(tempOutputPath);
            var directory = Path.GetDirectoryName(savePath);
            Directory.CreateDirectory(directory!);
            await File.WriteAllBytesAsync(savePath, outputBytes);
        }
        finally
        {
            File.Delete(tempInputPath);
            File.Delete(tempOutputPath);
        }
    }
}