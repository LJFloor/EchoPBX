using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoPBX.Data.Models;

/// <summary>
/// A call flow is a top-down script that describes what happens to a call, built visually
/// in the dashboard. Each flow is generated into its own <c>[callflow-{Slug}]</c> context
/// in extensions.conf.
/// </summary>
[Table("call_flows")]
public class CallFlow
{
    [Key] public int Id { get; set; }

    /// <summary>
    /// Unique, URL- and dialplan-safe identifier. Only a-z, A-Z, 0-9, underscore and dash.
    /// Used as the context name, so it must stay stable-ish.
    /// </summary>
    /// <example>main-menu</example>
    [MaxLength(64)]
    public string Slug { get; set; } = null!;

    /// <summary>
    /// Human readable name, shown in the dashboard only.
    /// </summary>
    [MaxLength(128)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Optional internal number that dials this flow from any extension. If null, the flow
    /// can only be reached through the test button.
    /// </summary>
    public int? InternalNumber { get; set; }

    /// <summary>
    /// The nodes and edges that make up the flow, serialized as JSON.
    /// See <see cref="CallFlowDefinition"/>.
    /// </summary>
    public string DefinitionJson { get; set; } = """{"nodes":[],"edges":[]}""";
}
