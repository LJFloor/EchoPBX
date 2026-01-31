using EchoPBX.Data;
using EchoPBX.Data.Dto;
using EchoPBX.Data.Workers.Asterisk;
using Microsoft.EntityFrameworkCore;

namespace EchoPBX.Repositories.ExtensionRead;

public class ExtensionReadRepository(EchoDbContext dbContext, IAsteriskWorker asterisk) : IExtensionReadRepository
{
    public async Task<Models.Extension[]> List()
    {
        var extensions = await Query().ToArrayAsync();
        var contacts = await asterisk.GetContacts();
        foreach (var extension in extensions)
        {
            extension.Connected = contacts.Any(x => x.Endpoint == extension.ExtensionNumber.ToString() && x.Status == ContactStatus.Available);
        }
        
        return extensions;
    }

    public async Task<Models.Extension?> Get(int extensionNumber)
    {
        var extension = await Query()
            .Where(x => x.ExtensionNumber == extensionNumber)
            .FirstOrDefaultAsync();

        if (extension is null)
        {
            return null;
        }
        
        var contacts = await asterisk.GetContacts();
        extension.Connected = contacts.Any(x => x.Endpoint == extension.ExtensionNumber.ToString() && x.Status == ContactStatus.Available);
        return extension;
    }

    private IQueryable<Models.Extension> Query()
    {
        return dbContext.Extensions.AsNoTracking().Select(x => new Models.Extension
        {
            ExtensionNumber = x.ExtensionNumber,
            DisplayName = x.DisplayName,
            MaxDevices = x.MaxDevices,
            Password = x.Password,
            OutgoingTrunkId = x.OutgoingTrunkId,
        });
    }
}