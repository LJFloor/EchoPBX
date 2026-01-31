using System.Diagnostics;
using EchoPBX.Data.Helpers;
using EchoPBX.Data.Models;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EchoPBX.Data.Workers.Cdr;

/// <summary>
/// A background service that monitors the Asterisk CDR CSV file and writes CDRs to the database.
/// </summary>
public class CdrWorker(IServiceProvider serviceProvider, ILogger<CdrWorker> logger) : ICdrWorker, IWorker
{
    private const string CsvFile = "/var/log/asterisk/cdr-csv/Master.csv";

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var directory = Path.GetDirectoryName(CsvFile)!;
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(CsvFile, "", stoppingToken);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    RedirectStandardOutput = true,
                    FileName = "/usr/bin/tail",
                    Arguments = $"-f {CsvFile}"
                }
            };

            process.Start();

            while (!process.HasExited)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    process.Kill();
                    break;
                }

                var line = await process.StandardOutput.ReadLineAsync(stoppingToken);
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                try
                {
                    var parts = CsvHelper.ParseCsvLine(line);
                    var cdr = new Data.Models.Cdr
                    {
                        Clid = parts[0],
                        Source = parts[1],
                        Destination = parts[2],
                        DestinationContext = parts[3],
                        ChannelName = parts[4],
                        DestinationChannel = parts[6],
                        LastAppExecuted = parts[7],
                        LastAppArguments = parts[8],
                        Start = DateTimeOffset.Parse(parts[9] + "Z").ToUnixTimeMilliseconds(),
                        Answer = string.IsNullOrEmpty(parts[10]) ? null : DateTimeOffset.Parse(parts[10] + "Z").ToUnixTimeMilliseconds(),
                        End = DateTimeOffset.Parse(parts[11] + "Z").ToUnixTimeMilliseconds(),
                        Duration = int.Parse(parts[12]),
                        BillSeconds = int.Parse(parts[13]),
                        Disposition = parts[14] switch
                        {
                            "ANSWERED" => CdrDisposition.Answered,
                            "BUSY" => CdrDisposition.Busy,
                            _ => CdrDisposition.NoAnswer,
                        },
                        AmaFlags = parts[15],
                        Direction = CallDirection.Internal
                    };


                    if (cdr.DestinationContext.StartsWith("from-trunk"))
                    {
                        cdr.Direction = CallDirection.Incoming;
                    }
                    else if (cdr.LastAppArguments.Contains("@trunk"))
                    {
                        cdr.Direction = CallDirection.Outgoing;
                    }

                    if (cdr is { Disposition: CdrDisposition.NoAnswer, Direction: CallDirection.Incoming })
                    {
                        // Ignore incoming no answer calls
                        continue;
                    }

                    using var scope = serviceProvider.CreateScope();
                    await using var dbContext = scope.ServiceProvider.GetRequiredService<EchoDbContext>();
                    await dbContext.BulkInsertAsync([cdr], cancellationToken: stoppingToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e.Message);
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
        }
    }
}