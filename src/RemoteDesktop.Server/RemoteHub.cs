using Microsoft.AspNetCore.SignalR;

namespace RemoteDesktop.Server;

public sealed class RemoteHub : Hub
{
    private const string AdminGroup = "admins";
    private readonly MachineRegistry _registry;

    public RemoteHub(MachineRegistry registry)
    {
        _registry = registry;
    }

    public async Task JoinAdmin()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
        await Clients.Caller.SendAsync("MachinesUpdated", _registry.GetMachines());
    }

    public async Task RegisterMachine(MachineInfo info)
    {
        if (string.IsNullOrWhiteSpace(info.MachineId))
        {
            throw new HubException("MachineId is required.");
        }

        _registry.Upsert(info, Context.ConnectionId);
        await BroadcastMachines();
    }

    public async Task Heartbeat(string machineId)
    {
        _registry.Heartbeat(machineId);
        await BroadcastMachines();
    }

    public async Task SendChatToMachine(string machineId, string message)
    {
        var connectionId = _registry.GetConnectionId(machineId);
        if (connectionId is null)
        {
            return;
        }

        var chat = new ChatMessage
        {
            MachineId = machineId,
            From = "admin",
            Message = message,
            SentAtUtc = DateTime.UtcNow
        };

        await Clients.Client(connectionId).SendAsync("ReceiveChatMessage", chat);
        await Clients.Group(AdminGroup).SendAsync("ReceiveChatMessage", chat);
    }

    public async Task SendChatToAdmin(string machineId, string message)
    {
        var chat = new ChatMessage
        {
            MachineId = machineId,
            From = "user",
            Message = message,
            SentAtUtc = DateTime.UtcNow
        };

        await Clients.Group(AdminGroup).SendAsync("ReceiveChatMessage", chat);
    }

    public async Task SendSupportRequestToAdmin(SupportRequest request)
    {
        request.SentAtUtc = DateTime.UtcNow;
        await Clients.Group(AdminGroup).SendAsync("ReceiveSupportRequest", request);
    }

    public async Task StartRemoteSession(string machineId)
    {
        var connectionId = _registry.GetConnectionId(machineId);
        if (connectionId is null)
        {
            return;
        }

        _registry.SetStreaming(machineId, true);
        await Clients.Client(connectionId).SendAsync("StartScreenStream");
        await BroadcastMachines();
    }

    public async Task StopRemoteSession(string machineId)
    {
        var connectionId = _registry.GetConnectionId(machineId);
        if (connectionId is null)
        {
            return;
        }

        _registry.SetStreaming(machineId, false);
        _registry.SetMouseLocked(machineId, false);
        await Clients.Client(connectionId).SendAsync("StopScreenStream");
        await Clients.Client(connectionId).SendAsync("SetMouseLock", false);
        await BroadcastMachines();
    }

    public async Task SendScreenFrame(ScreenFrame frame)
    {
        frame.SentAtUtc = DateTime.UtcNow;
        await Clients.Group(AdminGroup).SendAsync("ReceiveScreenFrame", frame);
    }

    public async Task SendCursorPosition(CursorPosition cursor)
    {
        cursor.SentAtUtc = DateTime.UtcNow;
        await Clients.Group(AdminGroup).SendAsync("ReceiveCursorPosition", cursor);
    }

    public async Task SendInputToMachine(string machineId, RemoteInputEvent inputEvent)
    {
        var connectionId = _registry.GetConnectionId(machineId);
        if (connectionId is null)
        {
            return;
        }

        await Clients.Client(connectionId).SendAsync("ReceiveInputEvent", inputEvent);
    }

    public async Task SetMouseLock(string machineId, bool locked)
    {
        var connectionId = _registry.GetConnectionId(machineId);
        if (connectionId is null)
        {
            return;
        }

        _registry.SetMouseLocked(machineId, locked);
        await Clients.Client(connectionId).SendAsync("SetMouseLock", locked);
        await BroadcastMachines();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_registry.MarkDisconnected(Context.ConnectionId))
        {
            await BroadcastMachines();
        }

        await base.OnDisconnectedAsync(exception);
    }

    private Task BroadcastMachines()
    {
        return Clients.Group(AdminGroup).SendAsync("MachinesUpdated", _registry.GetMachines());
    }
}
