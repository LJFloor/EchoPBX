namespace EchoPBX.Data.Helpers;

public abstract class FfmpegHelper
{
    /// <summary>
    /// Converts audio bytes to WAV format using FFmpeg and saves to the specified path.
    /// </summary>
    /// <remarks>
    /// Two files are written: the 8 kHz .wav at <paramref name="savePath"/>, and a 16 kHz copy
    /// next to it with the .wav16 extension. Asterisk picks whichever suits the call, so
    /// wideband calls (the webphone, HD phones) sound clear, while ordinary phone lines get
    /// the 8 kHz file without resampling.
    /// </remarks>
    /// <param name="bytes">The input audio bytes.</param>
    /// <param name="savePath">The path to save the converted WAV file.</param>
    public static async Task SaveAsWav(byte[] bytes, string savePath)
    {
        var tempInputPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempInputPath, bytes);
            Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

            await Convert(tempInputPath, savePath, 8000);
            await Convert(tempInputPath, WidebandPath(savePath), 16000);
        }
        finally
        {
            File.Delete(tempInputPath);
        }
    }

    /// <summary>
    /// Move a sound saved by <see cref="SaveAsWav"/>, together with its wideband copy.
    /// </summary>
    public static void MoveWav(string source, string target)
    {
        File.Move(source, target, overwrite: true);
        if (File.Exists(WidebandPath(source)))
        {
            File.Move(WidebandPath(source), WidebandPath(target), overwrite: true);
        }
    }

    /// <summary>
    /// Delete a sound saved by <see cref="SaveAsWav"/>, together with its wideband copy.
    /// </summary>
    public static void DeleteWav(string path)
    {
        File.Delete(path);
        File.Delete(WidebandPath(path));
    }

    private static string WidebandPath(string wavPath) => wavPath + "16";

    private static async Task Convert(string inputPath, string savePath, int sampleRate)
    {
        var tempOutputPath = Path.GetTempFileName();
        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "ffmpeg";
            process.StartInfo.Arguments = $"-nostdin -i \"{inputPath}\" -map 0:a -map_metadata -1 -ar {sampleRate} -ac 1 -f wav \"{tempOutputPath}\" -y";
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

            await File.WriteAllBytesAsync(savePath, await File.ReadAllBytesAsync(tempOutputPath));
        }
        finally
        {
            File.Delete(tempOutputPath);
        }
    }
}
