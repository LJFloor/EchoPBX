using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace EchoPBX.Data.Clients.Ami.Models;

/// <summary>
/// Represents an AMI event received from the Asterisk Manager Interface.
/// </summary>
/// <remarks>
/// The keys in the event data are case-insensitive, as per Asterisk AMI documentation.
/// </remarks>
public readonly struct AmiEvent : IReadOnlyDictionary<string, string>
{
    private readonly Dictionary<string, string> _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="AmiEvent"/> class.
    /// </summary>
    /// <param name="data">The key-value pairs representing the event data.</param>
    public AmiEvent(IDictionary<string, string> data)
    {
        _data = new Dictionary<string, string>(data, StringComparer.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Gets the type of the AMI event.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown if the "Event" key is not found in the event data.</exception>
    public string EventType => _data.TryGetValue("Event", out var value) ? value : throw new KeyNotFoundException("Event key not found in AMI event data.");

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        return _data.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => _data.Count;

    public bool ContainsKey(string key)
    {
        return _data.ContainsKey(key);
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
    {
        return _data.TryGetValue(key, out value);
    }

    public string this[string key] => _data[key];

    public IEnumerable<string> Keys => _data.Keys;

    public IEnumerable<string> Values => _data.Values;
}

/// <summary>
/// See <see href="https://docs.asterisk.org/Asterisk_20_Documentation/API_Documentation/AMI_Events/"/>
/// </summary>
public struct AmiEventType
{
    /// <summary>
    /// Raised when a new channel is created.
    /// </summary>
    public const string NewChannel = "Newchannel";

    /// <summary>
    /// Raised when a channel's state changes.
    /// </summary>
    public const string NewState = "Newstate";

    /// <summary>
    /// Raised when a channel is hung up.
    /// </summary>
    public const string Hangup = "Hangup";

    /// <summary>
    /// Raised when a dial action has started.
    /// </summary>
    public const string DialBegin = "DialBegin";

    /// <summary>
    /// Raised when a dial action has completed.
    /// </summary>
    public const string DialEnd = "DialEnd";

    /// <summary>
    /// Raised when a call pickup occurs.
    /// </summary>
    public const string Pickup = "Pickup";
    
    /// <summary>
    /// Raised when a bridge is created.
    /// </summary>
    public const string BridgeEnter = "BridgeEnter";
}