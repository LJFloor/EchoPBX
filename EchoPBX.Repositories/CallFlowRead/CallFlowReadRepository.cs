using EchoPBX.Data;
using EchoPBX.Data.Helpers;
using EchoPBX.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace EchoPBX.Repositories.CallFlowRead;

public class CallFlowReadRepository(EchoDbContext dbContext) : ICallFlowReadRepository
{
    public async Task<Models.CallFlow[]> List()
    {
        var callFlows = await dbContext.CallFlows
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Slug,
                x.Name,
                x.InternalNumber,
                x.DefinitionJson,
            })
            .ToArrayAsync();

        return callFlows.Select(x => new Models.CallFlow
        {
            Id = x.Id,
            Slug = x.Slug,
            Name = x.Name,
            InternalNumber = x.InternalNumber,
            Steps = CallFlowDefinition.Parse(x.DefinitionJson).Nodes.Count(n => n is not StartNode),
        }).ToArray();
    }

    public Task<Models.CallFlow?> GetBySlug(string slug) => Get(x => x.Slug == slug);

    public Task<Models.CallFlow?> GetById(int id) => Get(x => x.Id == id);

    private async Task<Models.CallFlow?> Get(System.Linq.Expressions.Expression<Func<CallFlow, bool>> predicate)
    {
        var callFlow = await dbContext.CallFlows
            .AsNoTracking()
            .Where(predicate)
            .Select(x => new
            {
                x.Id,
                x.Slug,
                x.Name,
                x.InternalNumber,
                x.DefinitionJson,
            })
            .FirstOrDefaultAsync();

        if (callFlow == null)
        {
            return null;
        }

        var definition = CallFlowDefinition.Parse(callFlow.DefinitionJson);

        // Uploaded sounds are stored as an absolute path without the extension, which is what
        // Playback() and Read() want. The dashboard needs a URL it can put in an <audio> tag instead.
        foreach (var node in definition.Nodes.OfType<ISoundNode>())
        {
            if (node.Kind == SoundKind.Upload && !string.IsNullOrEmpty(node.Sound))
            {
                node.Sound = StringHelper.BuildSoundUrl(node.Sound);
            }
        }

        return new Models.CallFlow
        {
            Id = callFlow.Id,
            Slug = callFlow.Slug,
            Name = callFlow.Name,
            InternalNumber = callFlow.InternalNumber,
            Steps = definition.Nodes.Count(n => n is not StartNode),
            Definition = definition,
            Trunks = await dbContext.Trunks
                .Where(x => x.CallFlowId == callFlow.Id && x.IncomingCallBehaviour == IncomingCallBehaviour.SendToCallFlow)
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToArrayAsync(),
        };
    }
}
