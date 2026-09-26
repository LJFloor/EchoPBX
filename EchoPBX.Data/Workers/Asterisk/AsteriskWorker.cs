using System.Diagnostics;
using System.Text.RegularExpressions;
using EchoPBX.Data.Clients.Ami;
using EchoPBX.Data.Clients.Ami.Models;
using EchoPBX.Data.Dto;
using EchoPBX.Data.Models;
using EchoPBX.Data.Services.Asterisk.Models;
using EchoPBX.Data.Services.CallFlows;
using EchoPBX.Data.Services.ContactSearch;
using EchoPBX.Data.Services.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EchoPBX.Data.Workers.Asterisk;

public partial class AsteriskWorker : IAsteriskWorker, IWorker
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AsteriskWorker> _logger;
    private readonly IAmiClient _amiClient;
    private readonly Process _asteriskProcess;
    private bool _asteriskStarted;
    private readonly CancellationTokenSource _monitorCts = new();

    /// <summary>
    /// Keeps two requests from writing the configuration files at the same time.
    /// </summary>
    private readonly SemaphoreSlim _configurationLock = new(1, 1);
    private readonly ISettingsService _settingsService;

    /// <summary>
    /// Full path to the asterisk executable
    /// </summary>
    private const string AsteriskPath = "/usr/sbin/asterisk";

    /// <summary>
    /// The directory that holds asterisk's config files
    /// </summary>
    private const string AsteriskConfigPath = "/etc/asterisk";

    /// <inheritdoc/>
    public bool IsReady { get; private set; } = false;

    /// <inheritdoc/>
    public List<OngoingCall> OngoingCalls
    {
        get
        {
            lock (_ongoingCalls) return [.._ongoingCalls];
        }
    }

    /// <summary>
    /// Only the AMI loop changes this list, but requests read it, so every access is locked and
    /// the outside world only ever gets a copy.
    /// </summary>
    private readonly List<OngoingCall> _ongoingCalls = [];

    /// <inheritdoc/>
    public event EventHandler<List<OngoingCall>>? OngoingCallsUpdated;

    public AsteriskWorker(IServiceProvider serviceProvider)
    {
        if (!File.Exists(AsteriskPath))
        {
            throw new FileNotFoundException("Asterisk not found!", AsteriskPath);
        }

        if (!Directory.Exists(AsteriskConfigPath))
        {
            throw new DirectoryNotFoundException($"Asterisk config directory not found! ({AsteriskConfigPath})");
        }

        // This worker is a singleton used from several threads at once, so every piece of work gets
        // its own scope and DbContext instead of sharing one
        _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var scope = serviceProvider.CreateScope();
        _logger = scope.ServiceProvider.GetRequiredService<ILogger<AsteriskWorker>>();
        _amiClient = scope.ServiceProvider.GetRequiredService<IAmiClient>();
        _settingsService = serviceProvider.GetRequiredService<ISettingsService>();
        _asteriskProcess = new Process
        {
            StartInfo = new ProcessStartInfo(AsteriskPath, "-f")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var asteriskProcesses = Process.GetProcessesByName("asterisk");
        if (asteriskProcesses.Length > 0)
        {
            _logger.LogWarning("Found existing asterisk process(es). Stopping them first...");
            foreach (var proc in asteriskProcesses)
            {
                _logger.LogInformation("Stopping existing asterisk process (PID: {Pid})...", proc.Id);
            }

            _logger.LogInformation("Running command: {Command}", $"{AsteriskPath} -rx \"core stop now\"");
            using var stopCommand = new Process();
            stopCommand.StartInfo = new ProcessStartInfo
            {
                FileName = AsteriskPath,
                Arguments = "-rx \"core stop now\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            stopCommand.Start();
            var stopOutput = stopCommand.StandardOutput.ReadToEndAsync(stoppingToken);
            var stopError = stopCommand.StandardError.ReadToEndAsync(stoppingToken);
            await stopCommand.WaitForExitAsync(stoppingToken);
            await Task.WhenAll(stopOutput, stopError);

            _logger.LogInformation("Stop command exited with code {ExitCode}", stopCommand.ExitCode);
            if (stopCommand.ExitCode != 0)
            {
                _logger.LogError("Failed to stop existing asterisk process(es). Exit code: {ExitCode}", stopCommand.ExitCode);
                throw new Exception("Asterisk is already running, and the attempt to stop it failed.");
            }

            // The stop command returns before asterisk has exited, while it may still hold its ports
            foreach (var proc in asteriskProcesses)
            {
                await WaitForExitOrKill(proc, TimeSpan.FromSeconds(10));
            }

            _logger.LogInformation("Existing asterisk process(es) stopped successfully.");
        }

        await WriteConfiguration();
        _logger.LogInformation("Starting asterisk process ({Path})...", _asteriskProcess.StartInfo.FileName);
        _asteriskProcess.Start();
        _asteriskStarted = true;

        // Nothing else reads stderr, and once its pipe fills up Asterisk blocks on its next write
        _ = Task.Run(async () =>
        {
            while (await _asteriskProcess.StandardError.ReadLineAsync(CancellationToken.None) is { } line)
            {
                if (!string.IsNullOrEmpty(line)) _logger.LogWarning("{Line}", line);
            }
        }, CancellationToken.None);

        _ = Task.Run(async () =>
        {
            await Task.Delay(500, stoppingToken);
            while (!_asteriskProcess.StandardOutput.EndOfStream)
            {
                var line = await _asteriskProcess.StandardOutput.ReadLineAsync(stoppingToken);
                if (string.IsNullOrEmpty(line)) continue;

                if (line.Contains("Asterisk Ready"))
                {
                    IsReady = true;
                }

                if (line.Contains("WARNING")) _logger.LogWarning(line);
                else if (line.Contains("ERROR")) _logger.LogError(line);
                else _logger.LogInformation(line);
            }

            IsReady = false;
            await _asteriskProcess.WaitForExitAsync(CancellationToken.None);
            if (_asteriskProcess.ExitCode == 0)
            {
                _logger.LogInformation("Asterisk process exited normally.");
            }
            else
            {
                _logger.LogError("Asterisk process has exited with code {ExitCode}", _asteriskProcess.ExitCode);
            }
        }, stoppingToken);

        _ = MonitorOngoingCalls();
    }

    /// <summary>
    /// Stops asterisk, so it does not keep running after EchoPBX exits.
    /// </summary>
    public async Task StopAsync()
    {
        if (!_asteriskStarted || _asteriskProcess.HasExited) return;

        IsReady = false;
        await _monitorCts.CancelAsync();
        _logger.LogInformation("Stopping asterisk...");

        // Not awaited: if asterisk hangs, the CLI command hangs with it
        _ = Execute("core stop now");
        await WaitForExitOrKill(_asteriskProcess, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Waits for a process to exit, and kills it if it takes longer than <paramref name="timeout"/>.
    /// </summary>
    private async Task WaitForExitOrKill(Process process, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Asterisk process (PID: {Pid}) did not exit in time, killing it", process.Id);
            process.Kill();
            await process.WaitForExitAsync();
        }
    }

    /// <inheritdoc />
    public async Task WaitUntilReady(CancellationToken cancellationToken = default)
    {
        while (!IsReady)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await Task.Delay(500, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task ApplyChanges()
    {
        await WriteConfiguration();
        await Execute("core reload");
    }

    /// <inheritdoc />
    public async Task<ContactDto[]> GetContacts()
    {
        var output = await Execute("pjsip show contacts");
        var contacts = new List<ContactDto>();
        var lines = output.Split('\n');
        var regex = ContactRegex();

        foreach (var line in lines)
        {
            var match = regex.Match(line);
            if (match.Success)
            {
                contacts.Add(new ContactDto
                {
                    Endpoint = match.Groups[1].Value,
                    Host = match.Groups[2].Value,
                    Status = line.Contains("Avail") ? ContactStatus.Available :
                        line.Contains("NonQual") ? ContactStatus.NonQualify :
                        ContactStatus.Unavailable
                });
            }
        }

        return contacts.ToArray();
    }

    /// <summary>
    /// Write the Asterisk configuration files based on the database settings.
    /// </summary>
    public async Task WriteConfiguration()
    {
        await _configurationLock.WaitAsync();
        try
        {
            await WriteConfigurationFiles();
        }
        finally
        {
            _configurationLock.Release();
        }
    }

    private async Task WriteConfigurationFiles()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EchoDbContext>();

        #region Data retrieval from database

        var extensions = await dbContext.Extensions.Select(x => new
        {
            x.DisplayName,
            x.ExtensionNumber,
            x.Password,
            x.OutgoingTrunkId,
            x.MaxDevices,
        }).ToArrayAsync();

        var trunks = await dbContext.Trunks.Select(x => new
        {
            x.Id,
            x.Host,
            x.Name,
            x.Username,
            x.Password,
            x.Codecs,
            x.Cid,
            x.IncomingCallBehaviour,
            Extensions = x.Extensions.Select(y => y.ExtensionNumber),
            Queue = x.Queue == null
                ? null
                : new
                {
                    x.Queue.Id,
                    x.Queue.Announcement,
                    x.Queue.Name
                },
            CallFlowSlug = x.CallFlow == null ? null : x.CallFlow.Slug,
        }).ToArrayAsync();

        var queues = await dbContext.Queues.Select(x => new
        {
            x.Id,
            x.MaxLength,
            x.Name,
            x.RetryInterval,
            x.Strategy,
            x.Timeout,
            x.WrapUpTime,
            x.MusicOnHold,
            x.Announcement,
            Extensions = x.Extensions
                .Where(y => y.Enabled)
                .OrderBy(y => y.Position)
                .Select(y => new
                {
                    y.ExtensionNumber,
                    y.Extension.DisplayName
                }).ToArray()
        }).ToArrayAsync();

        var callFlows = await dbContext.CallFlows.Select(x => new
        {
            x.Id,
            x.Slug,
            x.Name,
            x.InternalNumber,
            x.DefinitionJson,
        }).ToArrayAsync();

        #endregion

        var language = AsteriskLanguage();

        await WriteAmi();
        await WriteMusicOnHold();
        await WritePjsip();
        await WriteQueues();
        await WriteExtensions();
        return;

        #region Write to files

        async Task WriteExtensions()
        {
            var extensionLines = new List<string>
            {
                ";=============================================",
                "; Extensions",
                ";=============================================",
                "",
                "[system]",
                "exten => locate-extension,1,Dial(PJSIP/${EXTEN})",
                "exten => locate-extension,n,Hangup()",
                "",
            };

            // Entry points for the "test call" button, mirroring how queues are reached.
            foreach (var callFlow in callFlows)
            {
                var context = CallFlowDialplanBuilder.ContextName(callFlow.Slug);
                extensionLines.Add($"exten => {context},1,Goto({context},s,1)");
            }

            if (callFlows.Length > 0) extensionLines.Add("");

            foreach (var queue in queues)
            {
                extensionLines.Add("exten => queue-" + queue.Id + ",1,Answer()");
                extensionLines.Add("exten => queue-" + queue.Id + ",n,Wait(1)");
                if (!string.IsNullOrEmpty(queue.Announcement))
                {
                    extensionLines.Add("exten => queue-" + queue.Id + $",n,Playback({queue.Announcement})");
                }

                extensionLines.Add("exten => queue-" + queue.Id + ",n,Queue(queue-" + queue.Id + ")");
                extensionLines.Add("exten => queue-" + queue.Id + ",n,Hangup()");
                extensionLines.Add("");
            }

            extensionLines.AddRange([
                "[from-internal]",
            ]);

            foreach (var ext in extensions)
            {
                extensionLines.Add($"exten => {ext.ExtensionNumber},1,NoOp(\"Call to extension {ext.ExtensionNumber}\")");
                extensionLines.Add($" same => n,Dial(PJSIP/{ext.ExtensionNumber})");
                extensionLines.Add(" same => n,Hangup()");
                extensionLines.Add("");
            }

            foreach (var callFlow in callFlows.Where(x => x.InternalNumber != null))
            {
                var context = CallFlowDialplanBuilder.ContextName(callFlow.Slug);
                extensionLines.Add($"exten => {callFlow.InternalNumber},1,NoOp(\"Call to call flow {callFlow.Slug}\")");
                extensionLines.Add($" same => n,Goto({context},s,1)");
                extensionLines.Add("");
            }

            if (trunks.Length > 0)
            {
                extensionLines.Add(";=============================================");
                extensionLines.Add("; Incoming Trunks");
                extensionLines.Add(";=============================================");
                extensionLines.Add("");

                foreach (var trunk in trunks)
                {
                    extensionLines.Add($"[from-trunk-{trunk.Id}]");
                    extensionLines.Add($"exten => _X.,1,NoOp(\"Incoming call on trunk {trunk.Name}\")");

                    // Replace the caller's name with the matching contact, if there is one. The name
                    // is only ever measured with LEN(), never placed inside an expression, since
                    // quotes, colons or parentheses in it would break the expression.
                    extensionLines.Add(" same => n,GotoIf($[${LEN(${CALLERID(num)})} = 0]?lookup_done)");
                    extensionLines.Add($" same => n,Set(LKUP=${{CURL(http://127.0.0.1:{Constants.HttpPort}/api/contacts/lookup?num=${{CALLERID(num)}})}})"); // The contact's full name, or an empty string
                    extensionLines.Add(" same => n,GotoIf($[${LEN(${LKUP})} = 0]?lookup_done)");
                    extensionLines.Add(" same => n,Set(CALLERID(name)=${LKUP})");
                    extensionLines.Add(" same => n(lookup_done),NoOp()");

                    if (trunk.IncomingCallBehaviour == IncomingCallBehaviour.SendToQueue && trunk.Queue != null)
                    {
                        extensionLines.Add(" same => n,Answer()");
                        extensionLines.Add(" same => n,Wait(1)");
                        if (!string.IsNullOrEmpty(trunk.Queue.Announcement))
                        {
                            extensionLines.Add($" same => n,Playback({trunk.Queue.Announcement})");
                        }

                        extensionLines.Add(" same => n,Queue(queue-" + trunk.Queue.Id + ")");
                    }
                    else if (trunk.IncomingCallBehaviour == IncomingCallBehaviour.RingSpecificExtensions && trunk.Extensions.Any())
                    {
                        extensionLines.Add(" same => n,Dial(" + string.Join("&", trunk.Extensions.Select(x => $"PJSIP/{x}")) + ")");
                    }
                    else if (trunk.IncomingCallBehaviour == IncomingCallBehaviour.RingAllExtensions && extensions.Length > 0)
                    {
                        extensionLines.Add(" same => n,Dial(" + string.Join("&", extensions.Select(x => $"PJSIP/{x.ExtensionNumber}")) + ")");
                    }
                    else if (trunk.IncomingCallBehaviour == IncomingCallBehaviour.SendToCallFlow && trunk.CallFlowSlug != null)
                    {
                        extensionLines.Add($" same => n,Goto({CallFlowDialplanBuilder.ContextName(trunk.CallFlowSlug)},s,1)");
                    }

                    extensionLines.Add(" same => n,Hangup()");
                    extensionLines.Add("");
                }

                extensionLines.Add(";=============================================");
                extensionLines.Add("; Outgoing Trunks");
                extensionLines.Add(";=============================================");
                extensionLines.Add("");
                extensionLines.Add("; There are two contexts per trunk:");
                extensionLines.Add("; - out-trunk-<id> is used to dial out using this trunk");
                extensionLines.Add("; - using-trunk-<id> is used to include this trunk in the dial plan. This is the context that is set for the endpoints in pjsip.conf");
                extensionLines.Add(";");
                extensionLines.Add("; For some reason, we can't use do this:");
                extensionLines.Add(";");
                extensionLines.Add(";  [using-trunk-<id>]");
                extensionLines.Add(";  include => from-internal");
                extensionLines.Add(";  exten => _X.,1,Dial(PJSIP/${EXTEN}@trunk-<id>)");
                extensionLines.Add(";   same => n,Hangup()");
                extensionLines.Add(";");
                extensionLines.Add("; So we have to create a separate context for dialing out");

                extensionLines.Add("");

                foreach (var trunk in trunks)
                {
                    extensionLines.Add($"[out-trunk-{trunk.Id}]");
                    extensionLines.Add("exten => _X.,1,NoOp()");
                    if (OutgoingCallerId(trunk.Cid) is { } cid)
                    {
                        extensionLines.Add($" same => n,Set(CALLERID(num)={cid})");
                    }

                    extensionLines.Add(" same => n,Dial(PJSIP/${EXTEN}@trunk-" + trunk.Id + ")");
                    extensionLines.Add(" same => n,Hangup()");
                    extensionLines.Add("");
                    extensionLines.Add($"[using-trunk-{trunk.Id}]");
                    extensionLines.Add("include => from-internal");
                    extensionLines.Add("include => out-trunk-" + trunk.Id);
                    extensionLines.Add("");
                }
            }

            var queueIds = queues.Select(x => x.Id).ToHashSet();
            foreach (var callFlow in callFlows)
            {
                extensionLines.AddRange(CallFlowDialplanBuilder.Build(
                    callFlow.Id,
                    callFlow.Slug,
                    callFlow.Name,
                    CallFlowDefinition.Parse(callFlow.DefinitionJson),
                    queueIds));
            }

            const string extensionsFilePath = $"{AsteriskConfigPath}/extensions.conf";
            await File.WriteAllLinesAsync(extensionsFilePath, extensionLines);
            _logger.LogDebug("Wrote {LineCount} lines to {ConfigPath}", extensionLines.Count, extensionsFilePath);
        }

        async Task WriteQueues()
        {
            var queuesLines = new List<string>
            {
                "; EchoPBX Queue Configuration",
                "; This file is auto-generated every time you apply changes in the",
                "; EchoPBX web interface. Manual changes will be overwritten.",
                "",
            };

            foreach (var queue in queues)
            {
                queuesLines.AddRange([
                    ";=============================================",
                    $"; Queue {queue.Id} - " + queue.Name,
                    ";=============================================",
                    "",
                    $"[queue-{queue.Id}]",
                    "strategy=" + queue.Strategy,
                    "timeout=" + queue.Timeout,
                    "maxlen=" + queue.MaxLength,
                    "retry=" + queue.RetryInterval,
                    "musicclass=queue-" + queue.Id,
                    "wrapuptime=" + queue.WrapUpTime,
                    "announce-position=yes",
                    "announce-frequency=60",
                    "joinempty=yes",
                    "leavewhenempty=no",
                    "ringinuse=no",
                    ""
                ]);

                foreach (var ext in queue.Extensions)
                {
                    queuesLines.Add($"member => PJSIP/{ext.ExtensionNumber}" + (string.IsNullOrEmpty(ext.DisplayName) ? "" : $"   ; {ext.DisplayName}"));
                }

                queuesLines.Add("");
            }

            const string queuesFilePath = $"{AsteriskConfigPath}/queues.conf";
            await File.WriteAllLinesAsync(queuesFilePath, queuesLines);
            _logger.LogDebug("Wrote {LineCount} lines to {ConfigPath}", queuesLines.Count, queuesFilePath);
        }

        async Task WriteMusicOnHold()
        {
            var musicOnHoldLines = new List<string>
            {
                "; EchoPBX Music on Hold Configuration",
                "; This file is auto-generated every time you apply changes in the",
                "; EchoPBX web interface. Manual changes will be overwritten.",
                "",
                "[default]",
                "mode=files",
                "directory=/var/lib/asterisk/moh",
                "sort=alpha",
                ""
            };

            foreach (var queue in queues)
            {
                musicOnHoldLines.AddRange([
                    ";=============================================",
                    $"; Queue {queue.Id} - " + queue.Name,
                    ";=============================================",
                    "",
                    $"[queue-{queue.Id}]",
                    "mode=files",
                    !string.IsNullOrEmpty(queue.MusicOnHold) ? $"directory={queue.MusicOnHold}" : "directory=/var/lib/asterisk/moh",
                    "sort=alpha",
                    ""
                ]);
            }

            const string musicOnHoldFilePath = $"{AsteriskConfigPath}/musiconhold.conf";
            await File.WriteAllLinesAsync(musicOnHoldFilePath, musicOnHoldLines);
            _logger.LogDebug("Wrote {LineCount} lines to {ConfigPath}", musicOnHoldLines.Count, musicOnHoldFilePath);
        }

        async Task WritePjsip()
        {
            var pjsip = new List<string>
            {
                "; EchoPBX PJSIP Configuration",
                "; This file is auto-generated every time you apply changes in the",
                "; EchoPBX web interface. Manual changes will be overwritten.",
                "",
                ";===================================================================",
                "; Transport definition",
                ";===================================================================",
                "",
                "[transport-udp]",
                "type=transport",
                "protocol=udp",
                "bind=0.0.0.0:5060",
                "",
                "[transport-wss]",
                "type=transport",
                "protocol=wss",
                "bind=0.0.0.0",
                "",
                ";===================================================================",
                "; Global settings",
                ";===================================================================",
                "",
                "[global]",
                "type=global",
                $"user_agent=EchoPBX/{Constants.Version}"
            };

            foreach (var ext in extensions)
            {
                pjsip.Add(";===================================================================");
                pjsip.Add($"; Extension {ext.ExtensionNumber} - " + (ext.DisplayName ?? "No Name"));
                pjsip.Add(";===================================================================");
                pjsip.Add("");
                pjsip.Add($"[{ext.ExtensionNumber}]");
                pjsip.Add("type=endpoint");
                pjsip.Add($"language={language}");
                pjsip.Add("transport=transport-udp");
                pjsip.Add("disallow=all");
                pjsip.Add("allow=alaw,ulaw,g729,slin");
                pjsip.Add($"callerid={ext.DisplayName ?? ext.ExtensionNumber.ToString()} <{ext.ExtensionNumber}>");
                pjsip.Add($"auth=auth-{ext.ExtensionNumber}");
                pjsip.Add($"aors={ext.ExtensionNumber}");
                if (ext.OutgoingTrunkId != null) pjsip.Add("context=using-trunk-" + ext.OutgoingTrunkId);
                else pjsip.Add("context=from-internal");
                pjsip.Add("");
                pjsip.Add($"[auth-{ext.ExtensionNumber}]");
                pjsip.Add("type=auth");
                pjsip.Add("auth_type=userpass");
                pjsip.Add($"username={ext.ExtensionNumber}");
                pjsip.Add($"password={ext.Password}");
                pjsip.Add("");
                pjsip.Add($"[{ext.ExtensionNumber}]");
                pjsip.Add("type=aor");
                // Extensions saved before the dashboard had this field hold 0, which would lock every
                // device out. 5 is what they always got.
                pjsip.Add($"max_contacts={(ext.MaxDevices > 0 ? ext.MaxDevices : 5)}");
                pjsip.Add("qualify_frequency=10");
                pjsip.Add("");
            }

            if (trunks.Length > 0)
            {
                foreach (var trunk in trunks)
                {
                    pjsip.Add(";=============================================");
                    pjsip.Add($"; Trunk {trunk.Id} - " + trunk.Name);
                    pjsip.Add(";=============================================");
                    pjsip.Add("");

                    // Auth
                    pjsip.Add($"[trunk-{trunk.Id}-auth]");
                    pjsip.Add("type=auth");
                    pjsip.Add("auth_type=userpass");
                    pjsip.Add($"username={trunk.Username}");
                    pjsip.Add($"password={trunk.Password}");
                    pjsip.Add("");

                    // AOR
                    pjsip.Add($"[trunk-{trunk.Id}]");
                    pjsip.Add("type=aor");
                    pjsip.Add("contact=sip:" + trunk.Host);
                    pjsip.Add("qualify_frequency=10");
                    pjsip.Add("");

                    // Endpoint
                    pjsip.Add($"[trunk-{trunk.Id}]");
                    pjsip.Add("type=endpoint");
                    pjsip.Add($"context=from-trunk-{trunk.Id}");
                    pjsip.Add($"language={language}");
                    pjsip.Add("transport=transport-udp");
                    pjsip.Add("disallow=all");
                    var codecs = trunk.Codecs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    pjsip.Add("allow=" + (codecs.Length > 0 ? string.Join(',', codecs) : "alaw"));
                    pjsip.Add($"outbound_auth=trunk-{trunk.Id}-auth");
                    pjsip.Add("direct_media=no");
                    if (OutgoingCallerId(trunk.Cid) != null)
                    {
                        // from_user below fixes the From header, so the caller ID goes out as P-Asserted-Identity
                        pjsip.Add("send_pai=yes");
                        pjsip.Add("trust_id_outbound=yes");
                    }

                    pjsip.Add($"aors=trunk-{trunk.Id}");
                    pjsip.Add($"from_domain={trunk.Host}");
                    pjsip.Add($"from_user={trunk.Username}");
                    pjsip.Add("");

                    // Registration
                    pjsip.Add($"[trunk-{trunk.Id}-reg]");
                    pjsip.Add("type=registration");
                    pjsip.Add($"outbound_auth=trunk-{trunk.Id}-auth");
                    pjsip.Add($"server_uri=sip:{trunk.Host}");
                    pjsip.Add($"client_uri=sip:{trunk.Username}@{trunk.Host}");
                    pjsip.Add($"contact_user={trunk.Username}");
                    pjsip.Add("retry_interval=60");
                    pjsip.Add("");

                    // Identify
                    pjsip.Add($"[trunk-{trunk.Id}-identify]");
                    pjsip.Add("type=identify");
                    pjsip.Add($"endpoint=trunk-{trunk.Id}");
                    pjsip.Add($"match={trunk.Host}");
                    pjsip.Add("");
                }
            }

            const string filePath = $"{AsteriskConfigPath}/pjsip.conf";
            await File.WriteAllLinesAsync(filePath, pjsip);
            _logger.LogDebug("Wrote {LineCount} lines to {ConfigPath}", pjsip.Count, filePath);
        }

        async Task WriteAmi()
        {
            var amiFile = new[]
            {
                "; EchoPBX AMI Configuration",
                "; This file is auto-generated every time you apply changes in the",
                "; EchoPBX web interface. Manual changes will be overwritten.",
                "; Instead, drop your own file in /etc/asterisk/manager.d/ to add custom users.",
                "",
                "[general]",
                "enabled = yes",
                $"port = {AmiClient.Port}",
                "bindaddr = 127.0.0.1",
                "",
                $"[{AmiClient.Username}]",
                $"secret = {AmiClient.Secret}",
                "permit = 127.0.0.1/255.255.255.0",
                "read = all",
                "write = all",
                "",
                "#include \"manager.d/*.conf\"",
            };

            const string amiFilePath = $"{AsteriskConfigPath}/manager.conf";
            await File.WriteAllLinesAsync(amiFilePath, amiFile);
            _logger.LogDebug("Wrote {LineCount} lines to {ConfigPath}", amiFile.Length, amiFilePath);
        }

        #endregion
    }

    /// <summary>
    /// The language Asterisk plays its prompts in, from the AsteriskLanguage setting.
    /// </summary>
    private string AsteriskLanguage()
    {
        string? language = null;
        try
        {
            language = _settingsService.Get("AsteriskLanguage");
        }
        catch (KeyNotFoundException)
        {
            // Not seeded yet, fall back below
        }

        // Language codes look like "en" or "en_GB"; anything else would break pjsip.conf
        return !string.IsNullOrEmpty(language) && language.All(c => char.IsAsciiLetterOrDigit(c) || c == '_') ? language : "en";
    }

    /// <summary>
    /// The caller ID number to present on calls out of a trunk, or null to leave it to the provider.
    /// </summary>
    private static string? OutgoingCallerId(string? cid)
    {
        var cleaned = new string((cid ?? "").Where(c => char.IsAsciiDigit(c) || c == '+').ToArray());
        return cleaned.Any(char.IsAsciiDigit) ? cleaned : null;
    }

    /// <summary>
    /// Monitors ongoing calls by subscribing to AMI events. Updates the OngoingCalls list accordingly.
    /// </summary>
    private async Task MonitorOngoingCalls()
    {
        var cancellationToken = _monitorCts.Token;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await WaitUntilReady(cancellationToken);
                await _amiClient.ConnectAsync(cancellationToken);
                await ReadOngoingCalls();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Lost the AMI connection, reconnecting: {Message}", ex.Message);
            }

            _amiClient.Disconnect();

            // Whatever was going on is unknown now; the next events rebuild the list
            lock (_ongoingCalls) _ongoingCalls.Clear();
            OngoingCallsUpdated?.Invoke(this, OngoingCalls);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Reads AMI events until the connection drops, keeping the OngoingCalls list up to date.
    /// </summary>
    private async Task ReadOngoingCalls()
    {
        while (_amiClient.IsConnected)
        {
            // Outside the try, so a dropped connection ends the loop instead of being logged per event
            var amiEvent = await _amiClient.ReadNextEventAsync();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<EchoDbContext>();
                if (!amiEvent.TryGetValue("UniqueId", out var uniqueId) || !amiEvent.TryGetValue("LinkedId", out var linkedId))
                {
                    continue;
                }

                // Initialize
                if (amiEvent.EventType == AmiEventType.NewChannel && _ongoingCalls.All(x => x.UniqueId != uniqueId && x.UniqueId != linkedId))
                {
                    var trunkMatch = Regex.Match(amiEvent["Channel"], @"^PJSIP/trunk-(\d+)");
                    var extensionMatch = Regex.Match(amiEvent["Channel"], @"^PJSIP/(\d+)");

                    var direction = extensionMatch.Success
                        ? CallDirection.Outgoing
                        : trunkMatch.Success
                            ? CallDirection.Incoming
                            : CallDirection.Internal;

                    var call = new OngoingCall
                    {
                        Direction = direction,
                        UniqueId = uniqueId,
                        ExternalNumber = "",
                        StartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        State = CallState.Ringing,
                    };

                    if (direction == CallDirection.Incoming)
                    {
                        call.ExternalNumber = amiEvent.GetValueOrDefault("CallerIDNum", "Unknown");
                        call.TrunkId = int.Parse(trunkMatch.Groups[1].Value);
                    }
                    else if (direction == CallDirection.Outgoing)
                    {
                        call.ExtensionNumber = int.TryParse(extensionMatch.Groups[1].Value, out var extNum) ? extNum : null;
                        call.ExternalNumber = amiEvent.GetValueOrDefault("Exten", "Unknown");

                        if (call.ExternalNumber == "s")
                        {
                            // Internal system, most likely queue calling an agent, ignore
                            continue;
                        }

                        var caller = await dbContext.Extensions
                            .AsNoTracking()
                            .Where(x => x.ExtensionNumber == call.ExtensionNumber)
                            .Select(x => new { x.DisplayName, x.OutgoingTrunkId })
                            .FirstOrDefaultAsync();

                        if (!string.IsNullOrEmpty(caller?.DisplayName))
                        {
                            call.ExtensionName = caller.DisplayName;
                        }

                        // A number that is dialled internally never leaves through a trunk
                        if (await IsInternalNumber(dbContext, call.ExternalNumber))
                        {
                            call.Direction = CallDirection.Internal;
                        }
                        else
                        {
                            call.TrunkId = caller?.OutgoingTrunkId;
                        }
                    }

                    if (call.Direction != CallDirection.Internal)
                    {
                        var contact = await scope.ServiceProvider.GetRequiredService<IContactSearchService>().Search(call.ExternalNumber);
                        if (contact != null)
                        {
                            call.ExternalName = contact.Name;
                        }
                    }

                    lock (_ongoingCalls) _ongoingCalls.Add(call);
                    OngoingCallsUpdated?.Invoke(this, OngoingCalls);
                }

                // Waiting in a queue
                else if (amiEvent.EventType == AmiEventType.QueueCallerJoin)
                {
                    var call = _ongoingCalls.FirstOrDefault(x => x.UniqueId == uniqueId || x.UniqueId == linkedId);
                    var queueMatch = Regex.Match(amiEvent.GetValueOrDefault("Queue", ""), @"^queue-(\d+)$");
                    if (call == null || !queueMatch.Success) continue;

                    call.QueueId = int.Parse(queueMatch.Groups[1].Value);
                    OngoingCallsUpdated?.Invoke(this, OngoingCalls);
                }

                // Picking up
                else if (amiEvent.EventType == AmiEventType.BridgeEnter)
                {
                    var call = _ongoingCalls.FirstOrDefault(x => x.UniqueId == uniqueId || x.UniqueId == linkedId);
                    if (call == null || call.State == CallState.Ongoing) continue;

                    var extension = await dbContext.Extensions
                        .AsNoTracking()
                        .Where(x => x.ExtensionNumber.ToString() == amiEvent["CallerIDNum"])
                        .Select(x => new
                        {
                            x.ExtensionNumber,
                            x.DisplayName
                        }).FirstOrDefaultAsync();

                    if (extension == null) continue;

                    call.State = CallState.Ongoing;
                    call.PickupTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    call.ExtensionNumber = extension.ExtensionNumber;
                    call.ExtensionName = extension.DisplayName;
                    OngoingCallsUpdated?.Invoke(this, OngoingCalls);
                }

                // Call ended
                else if (amiEvent.EventType == AmiEventType.Hangup)
                {
                    var call = _ongoingCalls.FirstOrDefault(x => x.UniqueId == uniqueId || x.UniqueId == linkedId);
                    if (call == null) continue;

                    // it should be either the external number or the extension number that hangs up
                    // to avoid the queue timing out the extension to move to the next agent
                    var callerIdNum = amiEvent.GetValueOrDefault("CallerIDNum", "");
                    if (callerIdNum != call.ExternalNumber && callerIdNum != call.ExtensionNumber?.ToString())
                    {
                        continue;
                    }


                    lock (_ongoingCalls) _ongoingCalls.Remove(call);
                    OngoingCallsUpdated?.Invoke(this, OngoingCalls);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while monitoring ongoing calls");
            }
        }
    }

    /// <summary>
    /// Whether a dialled number belongs to an extension or a call flow rather than the outside world.
    /// </summary>
    private static async Task<bool> IsInternalNumber(EchoDbContext dbContext, string number)
    {
        if (!int.TryParse(number, out var internalNumber)) return false;

        return await dbContext.Extensions.AnyAsync(x => x.ExtensionNumber == internalNumber)
               || await dbContext.CallFlows.AnyAsync(x => x.InternalNumber == internalNumber);
    }

    /// <summary>
    /// Execute a command on the asterisk CLI and return the output.
    /// </summary>
    /// <param name="command">The command to execute</param>
    private async Task<string> Execute(string command)
    {
        var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = AsteriskPath,
            Arguments = $"-rx \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        process.Start();

        // Read both streams while the command runs; a full pipe would block it forever
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await error;
        var result = await output;
        process.Dispose();
        return result;
    }

    [GeneratedRegex(@"Contact:\s+([A-Za-z0-9-]+)\/sip:([^ \t;]+)")]
    private static partial Regex ContactRegex();
}