using EchoPBX.Data;
using EchoPBX.Data.Models;
using EchoPBX.Data.Workers.Asterisk;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

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
            QueueId = trunk.IncomingCallBehaviour == IncomingCallBehaviour.SendToQueue ? trunk.QueueId : null,
            CallFlowId = trunk.IncomingCallBehaviour == IncomingCallBehaviour.SendToCallFlow ? trunk.CallFlowId : null,
        };

        await dbContext.BulkInsertAsync([entity], new BulkConfig { SetOutputIdentity = true });

        if (entity.IncomingCallBehaviour == IncomingCallBehaviour.RingSpecificExtensions)
        {
            await dbContext.BulkInsertAsync(trunk.Extensions.Select(ext => new TrunkExtension
            {
                TrunkId = entity.Id,
                ExtensionNumber = ext
            }).ToList());
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
                .SetProperty(p => p.CallFlowId, trunk.IncomingCallBehaviour == IncomingCallBehaviour.SendToCallFlow ? trunk.CallFlowId : null)
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

        await asterisk.ApplyChanges();
    }
}
