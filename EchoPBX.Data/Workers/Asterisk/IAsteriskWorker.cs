using EchoPBX.Data.Dto;
using EchoPBX.Data.Services.Asterisk.Models;

namespace EchoPBX.Data.Workers.Asterisk;

public interface IAsteriskWorker
{
    /// <summary>
    /// Whether Asterisk is ready to accept commands.
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Get the list of ongoing calls.
    /// </summary>
    List<OngoingCall> OngoingCalls { get; }
    
    /// <summary>
    /// Event that is triggered when the list of ongoing calls is updated.
    /// </summary>
    public event EventHandler<List<OngoingCall>>? OngoingCallsUpdated;

    /// <summary>
    /// Waits until Asterisk is ready to accept commands.
    /// </summary>
    /// <param name="cancellationToken"></param>
    Task WaitUntilReady(CancellationToken cancellationToken = default);

    /// <summary>
    /// Write the configuration files and apply the changes by reloading Asterisk.
    /// </summary>
    Task ApplyChanges();

    /// <summary>
    /// Get the list of current contacts from Asterisk.
    /// </summary>
    /// <returns></returns>
    Task<ContactDto[]> GetContacts();

    /// <summary>
    /// Write the Asterisk configuration files based on the database settings.
    /// </summary>
    Task WriteConfiguration();
}