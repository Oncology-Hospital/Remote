using System.Collections.Concurrent;

namespace RemoteDesktop.Server;

public sealed class MachineRegistry
{
    private readonly ConcurrentDictionary<string, MachineRecord> _machines = new();

    public IReadOnlyList<MachineView> GetMachines()
    {
        return _machines.Values
            .OrderBy(machine => machine.Info.HostName)
            .Select(ToView)
            .ToList();
    }

    public string? GetConnectionId(string machineId)
    {
        if (!_machines.TryGetValue(machineId, out var record) || string.IsNullOrWhiteSpace(record.ConnectionId))
        {
            return null;
        }

        return record.ConnectionId;
    }

    public MachineView Upsert(MachineInfo info, string connectionId)
    {
        var record = _machines.AddOrUpdate(
            info.MachineId,
            _ => new MachineRecord(info, connectionId),
            (_, current) =>
            {
                current.Info = info;
                current.ConnectionId = connectionId;
                current.Status = "online";
                current.LastSeenUtc = DateTime.UtcNow;
                return current;
            });

        record.LastSeenUtc = DateTime.UtcNow;
        record.Status = "online";
        return ToView(record);
    }

    public MachineView? Heartbeat(string machineId)
    {
        if (!_machines.TryGetValue(machineId, out var record))
        {
            return null;
        }

        record.Status = "online";
        record.LastSeenUtc = DateTime.UtcNow;
        return ToView(record);
    }

    public MachineView? SetStreaming(string machineId, bool isStreaming)
    {
        if (!_machines.TryGetValue(machineId, out var record))
        {
            return null;
        }

        record.IsStreaming = isStreaming;
        record.LastSeenUtc = DateTime.UtcNow;
        return ToView(record);
    }

    public MachineView? SetMouseLocked(string machineId, bool locked)
    {
        if (!_machines.TryGetValue(machineId, out var record))
        {
            return null;
        }

        record.MouseLocked = locked;
        record.LastSeenUtc = DateTime.UtcNow;
        return ToView(record);
    }

    public bool MarkDisconnected(string connectionId)
    {
        var record = _machines.Values.FirstOrDefault(machine => machine.ConnectionId == connectionId);
        if (record is null)
        {
            return false;
        }

        record.Status = "offline";
        record.IsStreaming = false;
        record.MouseLocked = false;
        record.ConnectionId = "";
        record.LastSeenUtc = DateTime.UtcNow;
        return true;
    }

    private static MachineView ToView(MachineRecord record)
    {
        return new MachineView
        {
            MachineId = record.Info.MachineId,
            HostName = record.Info.HostName,
            IpAddress = record.Info.IpAddress,
            UserName = record.Info.UserName,
            ScreenWidth = record.Info.ScreenWidth,
            ScreenHeight = record.Info.ScreenHeight,
            Status = record.Status,
            IsStreaming = record.IsStreaming,
            MouseLocked = record.MouseLocked,
            LastSeenUtc = record.LastSeenUtc
        };
    }

    private sealed class MachineRecord
    {
        public MachineRecord(MachineInfo info, string connectionId)
        {
            Info = info;
            ConnectionId = connectionId;
            LastSeenUtc = DateTime.UtcNow;
        }

        public MachineInfo Info { get; set; }
        public string ConnectionId { get; set; }
        public string Status { get; set; } = "online";
        public bool IsStreaming { get; set; }
        public bool MouseLocked { get; set; }
        public DateTime LastSeenUtc { get; set; }
    }
}
