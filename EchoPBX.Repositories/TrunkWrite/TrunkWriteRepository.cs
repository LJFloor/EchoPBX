using EchoPBX.Data;
using EchoPBX.Data.Dto;
using EchoPBX.Data.Helpers;
using EchoPBX.Data.Models;
using EchoPBX.Data.Workers.Asterisk;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EchoPBX.Repositories.TrunkWrite;

public class TrunkWriteRepository(EchoDbContext dbContext, IAsteriskWorker asterisk) : ITrunkWriteRepository
{
    public async Task<int> Create(Models.Trunk trunk)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync();
        var entity = new Trunk
        {
            Name = trunk.Name,
            Host = trunk.Host,
            Username = trunk.Username,
            Password = trunk.Password,
            Codecs = string.Join(',', trunk.Codecs),
            Cid = trunk.Cid,
            IncomingCallBehaviour = trunk.IncomingCallBehaviour,
            QueueId = trunk.IncomingCallBehaviour == IncomingCallBehaviour.SendToQueue ? trunk.QueueId : null
        };

        await dbContext.BulkInsertAsync([entity], new BulkConfig { SetOutputIdentity = true });

        switch (entity.IncomingCallBehaviour)
        {
            case IncomingCallBehaviour.RingSpecificExtensions:
                await dbContext.BulkInsertAsync(trunk.Extensions.Select(ext => new TrunkExtension
                {
                    TrunkId = entity.Id,
                    ExtensionNumber = ext
                }).ToList());
                break;
            case IncomingCallBehaviour.DtmfMenu:
            {
                await SaveDtmfAnnouncement(entity.Id, trunk.DtmfAnnouncement);

                if (trunk.DtmfMenuEntries.Count > 0)
                {
                    await dbContext.BulkInsertAsync(trunk.DtmfMenuEntries.Select(e => new DtmfMenuEntry
                    {
                        TrunkId = entity.Id,
                        Digit = e.Digit,
                        QueueId = e.QueueId,
                    }).ToList());
                }

                break;
            }
        }

        await transaction.CommitAsync();
        await asterisk.ApplyChanges();
        return entity.Id;
    }

    public async Task Update(Models.Trunk trunk)
    {
        var updatedRows = await dbContext.Trunks
            .Where(x => x.Id == trunk.Id)
            .ExecuteUpdateAsync(x => x
                .SetProperty(p => p.Name, trunk.Name)
                .SetProperty(p => p.Host, trunk.Host)
                .SetProperty(p => p.Username, trunk.Username)
                .SetProperty(p => p.Password, trunk.Password)
                .SetProperty(p => p.Codecs, string.Join(',', trunk.Codecs))
                .SetProperty(p => p.Cid, trunk.Cid)
                .SetProperty(p => p.IncomingCallBehaviour, trunk.IncomingCallBehaviour)
                .SetProperty(p => p.QueueId, trunk.IncomingCallBehaviour == IncomingCallBehaviour.SendToQueue ? trunk.QueueId : null)
            );

        if (updatedRows == 0)
        {
            return;
        }

        await dbContext.Set<TrunkExtension>().Where(x => x.TrunkId == trunk.Id).ExecuteDeleteAsync();
        if (trunk.IncomingCallBehaviour == IncomingCallBehaviour.RingSpecificExtensions)
        {
            await dbContext.BulkInsertAsync(trunk.Extensions.Select(ext => new TrunkExtension
            {
                TrunkId = trunk.Id,
                ExtensionNumber = ext
            }).ToList());
        }

        // Handle DTMF menu entries
        await dbContext.Set<DtmfMenuEntry>().Where(x => x.TrunkId == trunk.Id).ExecuteDeleteAsync();

        if (trunk.IncomingCallBehaviour == IncomingCallBehaviour.DtmfMenu)
        {
            await SaveDtmfAnnouncement(trunk.Id, trunk.DtmfAnnouncement);

            if (trunk.DtmfMenuEntries.Count > 0)
            {
                await dbContext.BulkInsertAsync(trunk.DtmfMenuEntries.Select(e => new DtmfMenuEntry
                {
                    TrunkId = trunk.Id,
                    Digit = e.Digit,
                    QueueId = e.QueueId,
                }).ToList());
            }
        }
        else
        {
            // Clean up announcement file if switching away from DTMF
            await CleanupDtmfAnnouncement(trunk.Id);
        }

        await asterisk.ApplyChanges();
    }

    public async Task Delete(int id)
    {
        var rowsDeleted = await dbContext.Trunks
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync();

        if (rowsDeleted == 0)
        {
            throw new InvalidOperationException($"Trunk with ID {id} not found.");
        }

        // Clean up sound files
        var soundDir = Path.Combine(Constants.DataDirectory, "sounds", $"trunk-{id}");
        if (Directory.Exists(soundDir))
        {
            try
            {
                Directory.Delete(soundDir, true);
            }
            catch (Exception ex)
            {
                Log.Logger.Warning("Error deleting the sounds directory for trunk {TrunkId}: {message}", id, ex.Message);
            }
        }

        await asterisk.ApplyChanges();
    }

    private async Task SaveDtmfAnnouncement(int trunkId, string? announcement)
    {
        if (string.IsNullOrEmpty(announcement))
        {
            await CleanupDtmfAnnouncement(trunkId);
            return;
        }

        if (announcement.StartsWith("data:"))
        {
            var baseDir = Path.Combine(Constants.DataDirectory, "sounds", $"trunk-{trunkId}");
            Directory.CreateDirectory(baseDir);
            var announcementPath = Path.Combine(baseDir, "dtmf-announcement.wav");
            var content = UploadedFile.FromDataUrl(announcement).Content;
            await FfmpegHelper.SaveAsWav(content, announcementPath);

            await dbContext.Trunks
                .Where(x => x.Id == trunkId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.DtmfAnnouncement, announcementPath.Replace(".wav", ""))
                );
        }
        // If it doesn't start with "data:", it's an existing path -- leave it unchanged
    }

    private async Task CleanupDtmfAnnouncement(int trunkId)
    {
        await dbContext.Trunks
            .Where(x => x.Id == trunkId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.DtmfAnnouncement, (string?)null));

        var announcementPath = Path.Combine(Constants.DataDirectory, "sounds", $"trunk-{trunkId}", "dtmf-announcement.wav");
        if (File.Exists(announcementPath))
        {
            try
            {
                File.Delete(announcementPath);
            }
            catch (Exception ex)
            {
                Log.Logger.Warning("Error deleting DTMF announcement for trunk {TrunkId}: {message}", trunkId, ex.Message);
            }
        }
    }
}