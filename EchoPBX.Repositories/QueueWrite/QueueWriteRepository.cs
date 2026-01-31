using EchoPBX.Data;
using EchoPBX.Data.Dto;
using EchoPBX.Data.Helpers;
using EchoPBX.Data.Models;
using EchoPBX.Data.Services;
using EchoPBX.Data.Services.Asterisk;
using EchoPBX.Data.Workers.Asterisk;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EchoPBX.Repositories.QueueWrite;

public class QueueWriteRepository(EchoDbContext dbContext, IAsteriskWorker asterisk) : IQueueWriteRepository
{
    public async Task Update(Models.Queue queue)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync();
        var updatedRows = await dbContext.Queues
            .Where(x => x.Id == queue.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(q => q.Name, queue.Name.Trim())
                .SetProperty(q => q.Strategy, queue.Strategy.Trim())
                .SetProperty(q => q.Timeout, queue.Timeout)
                .SetProperty(q => q.MaxLength, queue.MaxLength)
                .SetProperty(q => q.WrapUpTime, queue.WrapUpTime)
                .SetProperty(q => q.RetryInterval, queue.RetryInterval)
            );

        if (updatedRows == 0)
        {
            throw new Exception("Queue not found");
        }

        await UpdateSounds(queue);
        await UpdateExtensions(queue);

        await transaction.CommitAsync();
        await asterisk.ApplyChanges();
    }

    public async Task Create(Models.Queue queue)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync();
        var newQueue = new Queue
        {
            Name = queue.Name.Trim(),
            Strategy = queue.Strategy.Trim(),
            Timeout = queue.Timeout,
            MaxLength = queue.MaxLength,
            WrapUpTime = queue.WrapUpTime,
            RetryInterval = queue.RetryInterval,
        };

        await dbContext.BulkInsertAsync([newQueue], new BulkConfig { SetOutputIdentity = true });
        queue.Id = newQueue.Id;

        await UpdateSounds(queue);
        await UpdateExtensions(queue);
        await transaction.CommitAsync();
        await asterisk.ApplyChanges();
    }

    public async Task Delete(int id)
    {
        await dbContext.Trunks
            .Where(x => x.QueueId == id && x.IncomingCallBehaviour == IncomingCallBehaviour.SendToQueue)
            .ExecuteUpdateAsync(set =>
                set.SetProperty(x => x.IncomingCallBehaviour, IncomingCallBehaviour.Ignore)
            );

        // Remove DTMF menu entries pointing to this queue
        await dbContext.Set<DtmfMenuEntry>().Where(x => x.QueueId == id).ExecuteDeleteAsync();

        // If a trunk with DtmfMenu behavior has no entries left, reset to Ignore
        var affectedTrunkIds = await dbContext.Trunks
            .Where(x => x.IncomingCallBehaviour == IncomingCallBehaviour.DtmfMenu)
            .Where(x => !x.DtmfMenuEntries.Any())
            .Select(x => x.Id)
            .ToArrayAsync();

        if (affectedTrunkIds.Length > 0)
        {
            await dbContext.Trunks
                .Where(x => affectedTrunkIds.Contains(x.Id))
                .ExecuteUpdateAsync(set =>
                    set.SetProperty(x => x.IncomingCallBehaviour, IncomingCallBehaviour.Ignore)
                       .SetProperty(x => x.DtmfAnnouncement, (string?)null)
                );
        }

        await dbContext.Set<QueueExtension>().Where(x => x.QueueId == id).ExecuteDeleteAsync();
        await dbContext.Queues.Where(x => x.Id == id).ExecuteDeleteAsync();
        await asterisk.ApplyChanges();
    }

    private async Task UpdateExtensions(Models.Queue queue)
    {
        var oldExtensions = await dbContext
            .Set<QueueExtension>()
            .Where(x => x.QueueId == queue.Id)
            .Select(x => new { x.ExtensionNumber, x.Enabled })
            .ToArrayAsync();

        await dbContext.Set<QueueExtension>().Where(x => x.QueueId == queue.Id).ExecuteDeleteAsync();
        var queueExtensions = queue.Extensions.Select((extension, i) => new QueueExtension
        {
            Enabled = oldExtensions.FirstOrDefault(x => x.ExtensionNumber == extension)?.Enabled ?? true,
            QueueId = queue.Id,
            ExtensionNumber = extension,
            Position = i
        }).ToList();

        await dbContext.BulkInsertAsync(queueExtensions);
    }

    private async Task UpdateSounds(Models.Queue queue)
    {
        var baseDir = Path.Combine(Constants.DataDirectory, "sounds", $"queue-{queue.Id}");
        if (queue.MusicOnHold.Length > 0)
        {
            // prepare the directory
            var musicDirectory = Path.Combine(baseDir, "hold-music");
            Directory.CreateDirectory(musicDirectory);

            var existingEntries = Directory.GetFiles(musicDirectory, "*.wav")
                .Select(Path.GetFileName)
                .Where(x => !string.IsNullOrEmpty(x))
                .Cast<string>()
                .ToArray(); // music-0.wav, music-1.wav, ...

            // delete any files removed from the list
            foreach (var existingEntry in existingEntries)
            {
                var found = queue.MusicOnHold.Any(t => t.EndsWith(".wav") && Path.GetFileName(t) == existingEntry);
                if (!found)
                {
                    try
                    {
                        File.Delete(Path.Combine(musicDirectory, existingEntry));
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Warning("Error deleting the file {File}: {message}", existingEntry, ex.Message);
                    }
                }
            }

            for (var i = 0; i < queue.MusicOnHold.Length; i++)
            {
                if (queue.MusicOnHold[i].EndsWith(".wav") && existingEntries.Contains(Path.GetFileName(queue.MusicOnHold[i])))
                {
                    // already exists, skip
                    continue;
                }

                if (queue.MusicOnHold[i].StartsWith("data:"))
                {
                    var content = UploadedFile.FromDataUrl(queue.MusicOnHold[i]).Content;
                    await FfmpegHelper.SaveAsWav(content, Path.Combine(musicDirectory, $"music-{i}.wav"));
                }
            }

            await dbContext.Queues
                .Where(x => x.Id == queue.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(q => q.MusicOnHold, musicDirectory)
                );
        }
        else
        {
            await dbContext.Queues
                .Where(x => x.Id == queue.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(q => q.MusicOnHold, (string?)null)
                );
        }


        if (string.IsNullOrEmpty(queue.Announcement))
        {
            await dbContext.Queues
                .Where(x => x.Id == queue.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(q => q.Announcement, (string?)null)
                );

            try
            {
                var announcementPath = Path.Combine(baseDir, "announcement.wav");
                if (File.Exists(announcementPath))
                {
                    File.Delete(announcementPath);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Warning("Error deleting the announcement file for queue {QueueId}: {message}", queue.Id, ex.Message);
            }
        }
        else if (queue.Announcement.StartsWith("data:"))
        {
            var announcementPath = Path.Combine(baseDir, "announcement.wav");
            var content = UploadedFile.FromDataUrl(queue.Announcement).Content;
            await FfmpegHelper.SaveAsWav(content, announcementPath);

            await dbContext.Queues
                .Where(x => x.Id == queue.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(q => q.Announcement, announcementPath.Replace(".wav", ""))
                );
        }


        // OPTIONAL: Clean up the sounds directory if it's empty
        if (Directory.Exists(baseDir))
        {
            var totalFiles = Directory.GetFiles(baseDir, "*.wav", SearchOption.AllDirectories).Length;
            if (totalFiles == 0)
            {
                try
                {
                    Directory.Delete(baseDir, true);
                }
                catch (Exception ex)
                {
                    Log.Logger.Information("Error deleting the sounds directory for queue {QueueId} since it is empty: {message}", queue.Id, ex.Message);
                }
            }
        }
    }
}