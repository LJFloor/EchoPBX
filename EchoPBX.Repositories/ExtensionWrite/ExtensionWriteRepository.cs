using EchoPBX.Data;
using EchoPBX.Data.Models;
using EchoPBX.Data.Services;
using EchoPBX.Data.Services.Asterisk;
using EchoPBX.Data.Workers.Asterisk;
using Microsoft.EntityFrameworkCore;

namespace EchoPBX.Repositories.ExtensionWrite;

public class ExtensionWriteRepository(EchoDbContext dbContext, IAsteriskWorker asterisk) : IExtensionWriteRepository
{
    public async Task Update(Models.Extension extension)
    {
        var rowsUpdated = await dbContext.Extensions
            .Where(x => x.ExtensionNumber == extension.ExtensionNumber)
            .ExecuteUpdateAsync(x => x
                .SetProperty(e => e.DisplayName, string.IsNullOrEmpty(extension.DisplayName) ? null : extension.DisplayName)
                .SetProperty(e => e.MaxDevices, extension.MaxDevices)
                .SetProperty(e => e.Password, extension.Password)
                .SetProperty(e => e.OutgoingTrunkId, extension.OutgoingTrunkId)
            );

        if (rowsUpdated == 1)
        {
            await asterisk.ApplyChanges();
        }
    }

    public async Task Delete(int extensionNumber)
    {
        var rowsDeleted = await dbContext.Extensions
            .Where(x => x.ExtensionNumber == extensionNumber)
            .ExecuteDeleteAsync();

        if (rowsDeleted == 1)
        {
            await asterisk.ApplyChanges();
        }
    }

    public async Task Create(Models.Extension extension)
    {
        dbContext.Extensions.Add(new Extension
        {
            DisplayName = extension.DisplayName,
            ExtensionNumber = extension.ExtensionNumber,
            Password = extension.Password,
            MaxDevices = extension.MaxDevices,
            OutgoingTrunkId = extension.OutgoingTrunkId,
        });

        await dbContext.SaveChangesAsync();
        await asterisk.ApplyChanges();
    }
}