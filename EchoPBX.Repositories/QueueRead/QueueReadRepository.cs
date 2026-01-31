using EchoPBX.Data;
using EchoPBX.Data.Services;
using EchoPBX.Repositories.QueueRead.Models;
using Microsoft.EntityFrameworkCore;

namespace EchoPBX.Repositories.QueueRead;

public class QueueReadRepository(EchoDbContext dbContext) : IQueueReadRepository
{
    public async Task<Queue[]> List()
    {
        return await dbContext.Queues.Select(q => new Models.Queue
        {
            Id = q.Id,
            Name = q.Name,
            Strategy = q.Strategy,
            Timeout = q.Timeout,
            MaxLength = q.MaxLength,
            WrapUpTime = q.WrapUpTime,
            RetryInterval = q.RetryInterval,
            Extensions = q.Extensions.OrderBy(x => x.Position).Select(m => m.ExtensionNumber).ToList()
        }).ToArrayAsync();
    }

    public async Task<Queue?> GetById(int id)
    {
        var queue = await dbContext.Queues.Select(q => new
        {
            q.Id,
            q.Name,
            q.Strategy,
            q.Timeout,
            q.MaxLength,
            q.WrapUpTime,
            q.RetryInterval,
            q.MusicOnHold,
            q.Announcement,
            Extensions = q.Extensions.OrderBy(x => x.Position).Select(m => m.ExtensionNumber).ToList()
        }).FirstOrDefaultAsync(x => x.Id == id);

        if (queue is null)
        {
            return null;
        }

        string[] musicOnHold = [];
        if (!string.IsNullOrEmpty(queue.MusicOnHold) && Directory.Exists(queue.MusicOnHold))
        {
            musicOnHold = Directory.GetFiles(queue.MusicOnHold, "*.wav")
                .Select(x => $"/sounds/queue-{queue.Id}/hold-music/{Path.GetFileName(x)}")
                .Order()
                .ToArray();
        }
        
        return new Queue
        {
            Id = queue.Id,
            Name = queue.Name,
            Strategy = queue.Strategy,
            Timeout = queue.Timeout,
            MaxLength = queue.MaxLength,
            WrapUpTime = queue.WrapUpTime,
            RetryInterval = queue.RetryInterval,
            Extensions = queue.Extensions,
            MusicOnHold = musicOnHold,
            Announcement = string.IsNullOrEmpty(queue.Announcement) ? null : $"/sounds/queue-{queue.Id}/{Path.GetFileName(queue.Announcement)}.wav"
        };
    }
}