using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EchoPBX.Data;
using EchoPBX.Data.Dto;
using EchoPBX.Data.Helpers;
using EchoPBX.Data.Models;
using EchoPBX.Data.Workers.Asterisk;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EchoPBX.Repositories.CallFlowWrite;

public partial class CallFlowWriteRepository(EchoDbContext dbContext, IAsteriskWorker asterisk) : ICallFlowWriteRepository
{
    /// <summary>
    /// Slugs become dialplan context names, so everything but characters Asterisk and URLs both
    /// handle without escaping is collapsed into a dash.
    /// </summary>
    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugCharactersRegex();

    private const int MaxSlugLength = 64;

    /// <summary>
    /// Reserved because the dashboard uses /admin/call-flows/new to create a flow.
    /// </summary>
    private const string ReservedSlug = "new";

    public async Task Create(Models.CallFlow callFlow)
    {
        await Validate(callFlow, existingId: null);

        var entity = new CallFlow
        {
            Slug = await GenerateSlug(callFlow.Name, existingId: null),
            Name = callFlow.Name.Trim(),
            InternalNumber = callFlow.InternalNumber,
        };

        dbContext.CallFlows.Add(entity);
        await dbContext.SaveChangesAsync();

        // The sound directory is keyed on the ID, so it can only be written once the row exists.
        await UpdateSounds(entity.Id, callFlow.Definition);
        entity.DefinitionJson = callFlow.Definition.Serialize();
        await dbContext.SaveChangesAsync();

        await asterisk.ApplyChanges();
    }

    public async Task Update(Models.CallFlow callFlow)
    {
        await Validate(callFlow, existingId: callFlow.Id);

        var entity = await dbContext.CallFlows.FirstOrDefaultAsync(x => x.Id == callFlow.Id);
        if (entity == null)
        {
            throw new Exception("Call flow not found");
        }

        await UpdateSounds(entity.Id, callFlow.Definition);

        // Only a rename moves the slug, so saving a flow does not change its URL for nothing.
        if (entity.Name != callFlow.Name.Trim())
        {
            entity.Slug = await GenerateSlug(callFlow.Name, existingId: entity.Id);
        }

        entity.Name = callFlow.Name.Trim();
        entity.InternalNumber = callFlow.InternalNumber;
        entity.DefinitionJson = callFlow.Definition.Serialize();
        await dbContext.SaveChangesAsync();

        await asterisk.ApplyChanges();
    }

    public async Task Delete(int id)
    {
        var deleted = await dbContext.CallFlows.Where(x => x.Id == id).ExecuteDeleteAsync();
        if (deleted == 0)
        {
            return;
        }

        var baseDirectory = SoundDirectory(id);
        if (Directory.Exists(baseDirectory))
        {
            try
            {
                Directory.Delete(baseDirectory, true);
            }
            catch (Exception ex)
            {
                Log.Logger.Warning("Error deleting the sounds directory for call flow {CallFlowId}: {Message}", id, ex.Message);
            }
        }

        await asterisk.ApplyChanges();
    }

    private static string SoundDirectory(int callFlowId) =>
        Path.Combine(Constants.DataDirectory, "sounds", $"callflow-{callFlowId}");

    /// <summary>
    /// Rejects anything that would produce a broken dialplan or an unreachable flow. This is the
    /// only validation layer there is, so it runs before every write.
    /// </summary>
    private async Task Validate(Models.CallFlow callFlow, int? existingId)
    {
        if (string.IsNullOrWhiteSpace(callFlow.Name))
        {
            throw new ValidationException("The name is required.");
        }

        if (callFlow.InternalNumber != null)
        {
            if (await dbContext.Extensions.AnyAsync(x => x.ExtensionNumber == callFlow.InternalNumber))
            {
                throw new ValidationException($"Extension {callFlow.InternalNumber} already exists.");
            }

            var numberTaken = await dbContext.CallFlows
                .AnyAsync(x => x.Id != existingId && x.InternalNumber == callFlow.InternalNumber);

            if (numberTaken)
            {
                throw new ValidationException($"Another call flow already uses internal number {callFlow.InternalNumber}.");
            }
        }

        ValidateDefinition(callFlow.Definition);

        foreach (var node in callFlow.Definition.Nodes.OfType<QueueNode>())
        {
            if (node.QueueId == null || !await dbContext.Queues.AnyAsync(x => x.Id == node.QueueId))
            {
                throw new ValidationException("Every \"Go to queue\" step needs a queue.");
            }
        }
    }

    /// <summary>
    /// Derives a slug from the name, adding a numeric suffix when another flow already has it.
    /// </summary>
    private async Task<string> GenerateSlug(string name, int? existingId)
    {
        var baseSlug = Slugify(name);
        var slug = baseSlug;

        for (var i = 2; slug == ReservedSlug || await dbContext.CallFlows.AnyAsync(x => x.Id != existingId && x.Slug == slug); i++)
        {
            var suffix = $"-{i}";
            slug = baseSlug[..Math.Min(baseSlug.Length, MaxSlugLength - suffix.Length)] + suffix;
        }

        return slug;
    }

    private static string Slugify(string name)
    {
        // Decomposing first turns accented letters into their base letter, so "Café" becomes "cafe".
        var builder = new StringBuilder();
        foreach (var c in name.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        var slug = NonSlugCharactersRegex().Replace(builder.ToString(), "-").Trim('-');
        if (slug.Length > MaxSlugLength)
        {
            slug = slug[..MaxSlugLength].TrimEnd('-');
        }

        // A name made up entirely of other characters (emoji, non-Latin scripts) leaves nothing.
        return slug.Length > 0 ? slug : "flow";
    }

    private static void ValidateDefinition(CallFlowDefinition definition)
    {
        if (definition.Nodes.Count(x => x is StartNode) != 1)
        {
            throw new ValidationException("A call flow must have exactly one start step.");
        }

        var ids = definition.Nodes.Select(x => x.Id).ToArray();
        if (ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count() != ids.Length)
        {
            throw new ValidationException("Every step needs its own unique id.");
        }

        var idSet = ids.ToHashSet();
        foreach (var edge in definition.Edges)
        {
            if (!idSet.Contains(edge.Source) || !idSet.Contains(edge.Target))
            {
                throw new ValidationException("A connection points at a step that no longer exists.");
            }
        }

        var duplicateOutput = definition.Edges
            .GroupBy(x => (x.Source, x.SourceHandle ?? ""))
            .Any(g => g.Count() > 1);

        if (duplicateOutput)
        {
            throw new ValidationException("A step cannot have more than one connection leaving the same output.");
        }

        if (definition.Nodes.Any(x => x is HangupNode or QueueNode && definition.Edges.Any(e => e.Source == x.Id)))
        {
            throw new ValidationException("Nothing can follow a step that ends the flow.");
        }

        foreach (var menu in definition.Nodes.OfType<MenuNode>())
        {
            ValidateMenu(menu, definition.Edges.Where(x => x.Source == menu.Id));
        }
    }

    private static void ValidateMenu(MenuNode menu, IEnumerable<CallFlowEdge> outgoing)
    {
        if (menu.Options.Count == 0)
        {
            throw new ValidationException("A phone menu needs at least one key.");
        }

        if (!menu.Options.All(MenuNode.IsKey) || menu.Options.Distinct().Count() != menu.Options.Count)
        {
            throw new ValidationException("A phone menu can only use the keys 0-9, * and #, each of them once.");
        }

        if (menu.Timeout is < 1 or > 60)
        {
            throw new ValidationException("A phone menu waits between 1 and 60 seconds for a key.");
        }

        if (menu.Attempts is < 1 or > 10)
        {
            throw new ValidationException("A phone menu plays its prompt between 1 and 10 times.");
        }

        if (outgoing.Any(x => x.SourceHandle != MenuNode.NoChoiceHandle && !menu.Options.Contains(x.SourceHandle!)))
        {
            throw new ValidationException("A phone menu has a branch for a key it does not offer.");
        }
    }

    /// <summary>
    /// Writes newly uploaded audio to disk, rewrites the definition to hold storage paths, and
    /// removes the files of steps that are gone.
    /// </summary>
    private static async Task UpdateSounds(int callFlowId, CallFlowDefinition definition)
    {
        var baseDirectory = SoundDirectory(callFlowId);
        var uploads = definition.Nodes
            .OfType<ISoundNode>()
            .Where(x => x.Kind == SoundKind.Upload)
            .ToArray();

        foreach (var node in uploads)
        {
            if (string.IsNullOrWhiteSpace(node.Sound))
            {
                node.Sound = null;
                continue;
            }

            if (node.Sound.StartsWith("data:"))
            {
                var savePath = Path.Combine(baseDirectory, $"{node.Id}.wav");
                var content = UploadedFile.FromDataUrl(node.Sound).Content;
                await FfmpegHelper.SaveAsWav(content, savePath);

                // Playback() takes the path without the extension.
                node.Sound = savePath[..^".wav".Length];
                continue;
            }

            // Anything else is a sound that was already saved, handed back to us as the
            // /sounds/... URL the read repository produced. Turn it back into a storage path.
            var fileName = Path.GetFileNameWithoutExtension(node.Sound);
            var existingPath = Path.Combine(baseDirectory, fileName);
            node.Sound = File.Exists($"{existingPath}.wav") ? existingPath : null;
        }

        // Built-in sounds are just Asterisk sound names; nothing to store.
        foreach (var node in definition.Nodes.OfType<ISoundNode>().Where(x => x.Kind == SoundKind.Builtin))
        {
            node.Sound = string.IsNullOrWhiteSpace(node.Sound) ? null : node.Sound.Trim();
        }

        if (!Directory.Exists(baseDirectory))
        {
            return;
        }

        var keep = uploads
            .Where(x => !string.IsNullOrEmpty(x.Sound))
            .Select(x => Path.GetFileName(x.Sound)!)
            .ToHashSet();

        foreach (var file in Directory.GetFiles(baseDirectory, "*.wav"))
        {
            if (keep.Contains(Path.GetFileNameWithoutExtension(file))) continue;

            try
            {
                File.Delete(file);
            }
            catch (Exception ex)
            {
                Log.Logger.Warning("Error deleting the sound file {File}: {Message}", file, ex.Message);
            }
        }

        if (Directory.GetFiles(baseDirectory).Length == 0)
        {
            try
            {
                Directory.Delete(baseDirectory, true);
            }
            catch (Exception ex)
            {
                Log.Logger.Information("Error deleting the empty sounds directory for call flow {CallFlowId}: {Message}", callFlowId, ex.Message);
            }
        }
    }
}
