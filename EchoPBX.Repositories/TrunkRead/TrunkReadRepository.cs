using EchoPBX.Data;
using EchoPBX.Data.Dto;
using EchoPBX.Data.Helpers;
using EchoPBX.Data.Services;
using EchoPBX.Data.Services.Asterisk;
using EchoPBX.Data.Workers.Asterisk;
using Microsoft.EntityFrameworkCore;

namespace EchoPBX.Repositories.TrunkRead;

public class TrunkReadRepository(EchoDbContext dbContext, IAsteriskWorker asterisk) : ITrunkReadRepository
{
    public async Task<Models.Trunk[]> List()
    {
        var trunks = await Query().ToArrayAsync();
        if (trunks.Length == 0)
        {
            return [];
        }
        
        var contacts = await asterisk.GetContacts();
        foreach (var trunk in trunks)
        {
            trunk.Connected = contacts.Any(x => x.Endpoint == $"trunk-{trunk.Id}" && x.Status == ContactStatus.Available);
        }
        
        return trunks;
    }

    public async Task<Models.Trunk?> Get(int id)
    {
        var trunk = await Query().FirstOrDefaultAsync(x => x.Id == id);
        if (trunk is null)
        {
            return null;
        }
        
        var contacts = await asterisk.GetContacts();
        trunk.Connected = contacts.Any(x => x.Endpoint == $"trunk-{trunk.Id}" && x.Status == ContactStatus.Available);
        return trunk;
    }

    private IQueryable<Models.Trunk> Query()
    {
        return dbContext.Trunks.AsNoTracking().Select(x => new Models.Trunk
        {
            Id = x.Id,
            Name = x.Name,
            Host = x.Host,
            Username = x.Username,
            Password = x.Password,
            Codecs = x.Codecs.Split(',', StringSplitOptions.RemoveEmptyEntries),
            Cid = x.Cid,
            Extensions = x.Extensions!.Select(te => te.ExtensionNumber).ToList(),
            QueueId = x.QueueId,
            IncomingCallBehaviour = x.IncomingCallBehaviour,
            DtmfAnnouncement = x.DtmfAnnouncement == null ? null : StringHelper.BuildSoundUrl(x.DtmfAnnouncement),
            DtmfMenuEntries = x.DtmfMenuEntries.Select(e => new Models.DtmfMenuEntryDto
            {
                Digit = e.Digit,
                QueueId = e.QueueId,
            }).ToList()
        });
    }
}