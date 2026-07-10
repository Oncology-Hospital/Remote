const language = new URLSearchParams(window.location.search).get("lang") === "en" ? "en" : "vi";
const translations = {
    vi: {
        appTitle: "Remote Desktop - Quản trị",
        brandTitle: "Quản trị phòng máy",
        machinesTitle: "Máy đang chạy",
        supportTitle: "Cần hỗ trợ",
        connected: "Đã kết nối",
        reconnecting: "Đang kết nối lại",
        disconnected: "Chưa kết nối",
        connectFailed: "Kết nối thất bại",
        refresh: "Làm mới",
        searchPlaceholder: "Tìm kiếm tên máy, IP, người dùng",
        noMachineSelected: "Chưa chọn máy",
        noTarget: "Chưa chọn máy",
        chat: "Trò chuyện",
        chatPlaceholder: "Nhắn đến máy đã chọn",
        send: "Gửi",
        emptySelect: "Chọn một máy đang online rồi nhấn Kết nối",
        waitingFrames: "Đang chờ hình ảnh màn hình...",
        remoteStopped: "Phiên xem màn hình đã dừng",
        pressConnect: "Nhấn Kết nối để bắt đầu xem màn hình",
        noOnline: "Chưa có máy nào đang online.",
        noMatches: "Không có máy phù hợp.",
        noSupport: "Chưa có máy nào gọi hỗ trợ.",
        connect: "Kết nối (F5)",
        disconnect: "Ngắt kết nối (F5)",
        remote: "Điều khiển (F2)",
        viewOnly: "Chỉ xem (F2)",
        lockMouse: "Khóa chuột (F1)",
        unlockMouse: "Mở khóa chuột (F1)",
        fullscreen: "Toàn màn hình (F11)",
        exitFullscreen: "Thoát toàn màn hình (F11)",
        connectTitle: "Kết nối phiên xem màn hình (F5)",
        disconnectTitle: "Ngắt phiên xem màn hình (F5)",
        remoteTitle: "Điều khiển chuột và bàn phím trên máy này (F2)",
        viewOnlyTitle: "Dừng điều khiển máy này (F2)",
        lockTitle: "Khóa hoặc mở khóa chuột tại máy người dùng (F1)",
        lockDisabledTitle: "Bật Remote (F2) trước khi khóa chuột người dùng"
    },
    en: {
        appTitle: "Remote Desktop Admin",
        brandTitle: "Room Admin",
        machinesTitle: "Online",
        supportTitle: "Needs support",
        connected: "Connected",
        reconnecting: "Reconnecting",
        disconnected: "Disconnected",
        connectFailed: "Connect failed",
        refresh: "Refresh",
        searchPlaceholder: "Search machine name, IP, user",
        noMachineSelected: "No machine selected",
        noTarget: "No target",
        chat: "Chat",
        chatPlaceholder: "Message to selected machine",
        send: "Send",
        emptySelect: "Select an online machine and press Connect",
        waitingFrames: "Waiting for screen frames...",
        remoteStopped: "Remote session stopped",
        pressConnect: "Press Connect to start remote view",
        noOnline: "No machines are online.",
        noMatches: "No matching machines.",
        noSupport: "No support requests.",
        connect: "Connect (F5)",
        disconnect: "Disconnect (F5)",
        remote: "Remote (F2)",
        viewOnly: "View only (F2)",
        lockMouse: "Lock mouse (F1)",
        unlockMouse: "Unlock mouse (F1)",
        fullscreen: "Fullscreen (F11)",
        exitFullscreen: "Exit fullscreen (F11)",
        connectTitle: "Connect remote session (F5)",
        disconnectTitle: "Disconnect remote session (F5)",
        remoteTitle: "Control mouse and keyboard on this machine (F2)",
        viewOnlyTitle: "Stop controlling this machine (F2)",
        lockTitle: "Lock or unlock the user's local mouse (F1)",
        lockDisabledTitle: "Enable Remote (F2) before locking the user's mouse"
    }
};

function t(key) {
    return translations[language][key] ?? key;
}

const state = {
    machines: [],
    selectedMachineId: null,
    selectedMachine: null,
    remoteConnected: false,
    remoteControlEnabled: false,
    lastMouseMoveAt: 0,
    lastCursor: null,
    machineSearch: "",
    activeSidebarTab: "support",
    supportRequests: []
};

const machineList = document.getElementById("machineList");
const machineSearchInput = document.getElementById("machineSearchInput");
const brandTitle = document.getElementById("brandTitle");
const supportTabLabel = document.getElementById("supportTabLabel");
const supportTab = document.getElementById("supportTab");
const onlineTab = document.getElementById("onlineTab");
const supportBadge = document.getElementById("supportBadge");
const supportToast = document.getElementById("supportToast");
const selectedName = document.getElementById("selectedName");
const selectedMeta = document.getElementById("selectedMeta");
const connectionStatus = document.getElementById("connectionStatus");
const refreshButton = document.getElementById("refreshButton");
const connectButton = document.getElementById("connectButton");
const remoteControlButton = document.getElementById("remoteControlButton");
const lockButton = document.getElementById("lockButton");
const fullscreenButton = document.getElementById("fullscreenButton");
const remotePanel = document.getElementById("remotePanel");
const screenStage = document.getElementById("screenStage");
const screenImage = document.getElementById("screenImage");
const remoteCursor = document.getElementById("remoteCursor");
const emptyScreen = document.getElementById("emptyScreen");
const chatTarget = document.getElementById("chatTarget");
const chatLog = document.getElementById("chatLog");
const chatForm = document.getElementById("chatForm");
const chatInput = document.getElementById("chatInput");
const chatTitle = document.getElementById("chatTitle");
const chatSendButton = document.getElementById("chatSendButton");

applyLanguage();

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/remoteHub")
    .withAutomaticReconnect()
    .build();

connection.on("MachinesUpdated", machines => {
    state.machines = machines;
    state.selectedMachine = machines.find(machine => machine.machineId === state.selectedMachineId) ?? null;
    renderMachines();
    renderSelection();
});

connection.on("ReceiveChatMessage", chat => {
    if (state.selectedMachineId && chat.machineId !== state.selectedMachineId) {
        return;
    }

    const line = document.createElement("div");
    line.className = "chat-line";
    line.innerHTML = `<strong>${escapeHtml(chat.from)}:</strong> ${escapeHtml(chat.message)}`;
    chatLog.appendChild(line);
    chatLog.scrollTop = chatLog.scrollHeight;
});

connection.on("ReceiveScreenFrame", frame => {
    if (!state.remoteConnected || frame.machineId !== state.selectedMachineId) {
        return;
    }

    screenImage.src = `data:image/jpeg;base64,${frame.base64Jpeg}`;
    screenStage.style.display = "inline-block";
    screenImage.style.display = "block";
    emptyScreen.style.display = "none";
    renderCursor();
});

connection.on("ReceiveCursorPosition", cursor => {
    if (cursor.machineId !== state.selectedMachineId) {
        return;
    }

    state.lastCursor = {
        ...(state.lastCursor ?? {}),
        ...cursor
    };
    renderCursor();
});

connection.on("ReceiveSupportRequest", request => {
    const current = state.supportRequests.filter(item => item.machineId !== request.machineId);
    state.supportRequests = [{ ...request, unread: true }, ...current];
    state.activeSidebarTab = "support";
    renderMachines();
    renderSidebarTabs();
    showSupportToast(request);
});

connection.onreconnecting(() => {
    connectionStatus.textContent = t("reconnecting");
});

connection.onreconnected(async () => {
    connectionStatus.textContent = t("connected");
    await connection.invoke("JoinAdmin");
});

connection.onclose(() => {
    connectionStatus.textContent = t("disconnected");
});

refreshButton.addEventListener("click", () => connection.invoke("JoinAdmin"));
connectButton.addEventListener("click", toggleRemote);
remoteControlButton.addEventListener("click", () => toggleRemoteControl());
lockButton.addEventListener("click", toggleMouseLock);
fullscreenButton.addEventListener("click", toggleFullscreen);
supportTab.addEventListener("click", () => setSidebarTab("support"));
onlineTab.addEventListener("click", () => setSidebarTab("online"));
window.addEventListener("resize", renderCursor);
updateRemoteToggleButton();
updateRemoteControlButton();
updateLockButton();
updateFullscreenButton();
renderSidebarTabs();

machineSearchInput.addEventListener("input", () => {
    state.machineSearch = machineSearchInput.value.trim().toLowerCase();
    renderMachines();
});

machineSearchInput.addEventListener("keydown", event => {
    if (event.key !== "Escape") {
        return;
    }

    machineSearchInput.value = "";
    state.machineSearch = "";
    renderMachines();
});

document.addEventListener("keydown", event => {
    if (event.key === "F5") {
        event.preventDefault();
        event.stopPropagation();
        toggleRemote();
        return;
    }

    if (event.key === "F2") {
        event.preventDefault();
        event.stopPropagation();
        toggleRemoteControl();
        return;
    }

    if (event.key === "F11") {
        event.preventDefault();
        event.stopPropagation();
        toggleFullscreen();
    }
}, true);

document.addEventListener("fullscreenchange", () => {
    updateFullscreenButton();
    renderCursor();
});

chatForm.addEventListener("submit", async event => {
    event.preventDefault();
    const message = chatInput.value.trim();
    if (!message || !state.selectedMachineId) {
        return;
    }

    chatInput.value = "";
    await connection.invoke("SendChatToMachine", state.selectedMachineId, message);
});

screenImage.addEventListener("mousemove", event => {
    const now = performance.now();
    if (now - state.lastMouseMoveAt < 45) {
        return;
    }

    state.lastMouseMoveAt = now;
    sendPointerEvent("mouseMove", event);
});

screenImage.addEventListener("mousedown", event => {
    remotePanel.focus();
    sendPointerEvent("mouseDown", event);
});

screenImage.addEventListener("mouseup", event => sendPointerEvent("mouseUp", event));
screenImage.addEventListener("contextmenu", event => {
    event.preventDefault();
    remotePanel.focus();
});
screenImage.addEventListener("wheel", event => {
    event.preventDefault();
    sendPointerEvent("mouseWheel", event);
}, { passive: false });

remotePanel.addEventListener("keydown", async event => {
    if (!state.remoteConnected || !state.selectedMachineId) {
        return;
    }

    event.preventDefault();
    if (event.key === "F1" && state.remoteControlEnabled) {
        await toggleMouseLock();
        return;
    }

    if (!state.remoteControlEnabled) {
        return;
    }

    await sendInput({ type: "keyDown", keyCode: event.keyCode || event.which || 0 });
});

remotePanel.addEventListener("keyup", async event => {
    if (
        !state.remoteConnected ||
        !state.selectedMachineId ||
        ["F1", "F2", "F5", "F11"].includes(event.key) ||
        !state.remoteControlEnabled
    ) {
        return;
    }

    event.preventDefault();
    await sendInput({ type: "keyUp", keyCode: event.keyCode || event.which || 0 });
});

async function start() {
    try {
        await connection.start();
        connectionStatus.textContent = t("connected");
        await connection.invoke("JoinAdmin");
    } catch (error) {
        connectionStatus.textContent = t("connectFailed");
        console.error(error);
        setTimeout(start, 1500);
    }
}

async function startRemote() {
    if (!state.selectedMachineId) {
        return;
    }

    state.remoteConnected = true;
    state.remoteControlEnabled = false;
    updateRemoteToggleButton();
    updateRemoteControlButton();
    updateLockButton();
    updateFullscreenButton();
    state.lastCursor = null;
    screenStage.style.display = "none";
    screenImage.style.display = "none";
    remoteCursor.removeAttribute("src");
    remoteCursor.style.display = "none";
    emptyScreen.style.display = "block";
    emptyScreen.textContent = t("waitingFrames");
    remotePanel.focus();
    await connection.invoke("StartRemoteSession", state.selectedMachineId);
}

async function stopRemote() {
    if (!state.selectedMachineId) {
        return;
    }

    state.remoteConnected = false;
    state.remoteControlEnabled = false;
    updateRemoteToggleButton();
    updateRemoteControlButton();
    updateLockButton();
    updateFullscreenButton();
    state.lastCursor = null;
    screenImage.removeAttribute("src");
    screenStage.style.display = "none";
    screenImage.style.display = "none";
    remoteCursor.removeAttribute("src");
    remoteCursor.style.display = "none";
    emptyScreen.style.display = "block";
    emptyScreen.textContent = t("remoteStopped");
    await connection.invoke("StopRemoteSession", state.selectedMachineId);

    if (document.fullscreenElement === remotePanel) {
        await document.exitFullscreen();
    }
}

async function toggleRemote() {
    if (state.remoteConnected) {
        await stopRemote();
        return;
    }

    await startRemote();
}

async function toggleRemoteControl() {
    if (!state.remoteConnected || !state.selectedMachineId) {
        return;
    }

    state.remoteControlEnabled = !state.remoteControlEnabled;
    if (!state.remoteControlEnabled && state.selectedMachine?.mouseLocked) {
        state.selectedMachine.mouseLocked = false;
        await connection.invoke("SetMouseLock", state.selectedMachineId, false);
    }

    updateRemoteControlButton();
    updateLockButton();
    remotePanel.focus();
}

async function toggleFullscreen() {
    if (!state.remoteConnected) {
        return;
    }

    if (document.fullscreenElement) {
        await document.exitFullscreen();
        return;
    }

    await remotePanel.requestFullscreen();
}

async function toggleMouseLock() {
    if (!state.selectedMachineId || !state.selectedMachine) {
        return;
    }

    const next = !state.selectedMachine.mouseLocked;
    await connection.invoke("SetMouseLock", state.selectedMachineId, next);
}

async function sendPointerEvent(type, event) {
    if (!state.remoteConnected || !state.remoteControlEnabled || !state.selectedMachineId || !state.selectedMachine) {
        return;
    }

    const point = getRemotePoint(event);
    state.lastCursor = {
        ...(state.lastCursor ?? {}),
        machineId: state.selectedMachineId,
        x: point.x,
        y: point.y
    };
    renderCursor();

    await sendInput({
        type,
        x: point.x,
        y: point.y,
        button: buttonName(event.button),
        delta: Math.trunc(event.deltaY || 0)
    });
}

function getRemotePoint(event) {
    const rect = screenImage.getBoundingClientRect();
    const xRatio = clamp((event.clientX - rect.left) / rect.width, 0, 1);
    const yRatio = clamp((event.clientY - rect.top) / rect.height, 0, 1);

    return {
        x: Math.round(xRatio * state.selectedMachine.screenWidth),
        y: Math.round(yRatio * state.selectedMachine.screenHeight)
    };
}

function renderCursor() {
    if (
        !state.lastCursor ||
        !state.selectedMachine ||
        state.lastCursor.isVisible === false ||
        screenImage.style.display === "none"
    ) {
        remoteCursor.style.display = "none";
        return;
    }

    if (state.lastCursor.imageBase64Png) {
        remoteCursor.src = `data:image/png;base64,${state.lastCursor.imageBase64Png}`;
    }

    if (!remoteCursor.getAttribute("src")) {
        remoteCursor.style.display = "none";
        return;
    }

    const remoteWidth = state.selectedMachine.screenWidth || 1;
    const remoteHeight = state.selectedMachine.screenHeight || 1;
    const displayWidth = screenImage.clientWidth || 1;
    const displayHeight = screenImage.clientHeight || 1;
    const scaleX = displayWidth / remoteWidth;
    const scaleY = displayHeight / remoteHeight;
    const scale = Math.min(scaleX, scaleY);
    const cursorWidth = Math.max(1, (state.lastCursor.cursorWidth || remoteCursor.naturalWidth || 32) * scale);
    const cursorHeight = Math.max(1, (state.lastCursor.cursorHeight || remoteCursor.naturalHeight || 32) * scale);
    const hotspotX = (state.lastCursor.hotspotX || 0) * scale;
    const hotspotY = (state.lastCursor.hotspotY || 0) * scale;
    const x = clamp(state.lastCursor.x / remoteWidth, 0, 1) * displayWidth - hotspotX;
    const y = clamp(state.lastCursor.y / remoteHeight, 0, 1) * displayHeight - hotspotY;

    remoteCursor.style.display = "block";
    remoteCursor.style.width = `${cursorWidth}px`;
    remoteCursor.style.height = `${cursorHeight}px`;
    remoteCursor.style.transform = `translate(${Math.round(x)}px, ${Math.round(y)}px)`;
}

function sendInput(input) {
    return connection.invoke("SendInputToMachine", state.selectedMachineId, input);
}

function renderMachines() {
    machineList.innerHTML = "";
    renderSidebarTabs();

    if (state.activeSidebarTab === "support") {
        renderSupportRequests();
        return;
    }

    const machines = getFilteredMachines();

    for (const machine of machines) {
        const item = document.createElement("button");
        item.className = `machine${machine.machineId === state.selectedMachineId ? " active" : ""}`;
        item.type = "button";
        item.innerHTML = `
            <div class="machine-title">
                <span>${escapeHtml(machine.hostName || machine.machineId)}</span>
                <span class="badge ${machine.status === "online" ? "online" : ""}">${escapeHtml(machine.status)}</span>
            </div>
            <div class="machine-meta">
                ${escapeHtml(machine.ipAddress)}<br>
                ${escapeHtml(machine.userName)} · ${machine.screenWidth}x${machine.screenHeight}
            </div>
        `;
        item.querySelector(".badge").textContent = statusText(machine.status);
        item.querySelector(".machine-meta").innerHTML = `${escapeHtml(machine.ipAddress)}<br>${escapeHtml(machine.userName)} · ${machine.screenWidth}x${machine.screenHeight}`;
        item.querySelector(".machine-meta").innerHTML = `${escapeHtml(machine.ipAddress)}<br>${escapeHtml(machine.userName)} - ${machine.screenWidth}x${machine.screenHeight}`;
        item.addEventListener("click", () => selectMachine(machine.machineId));
        machineList.appendChild(item);
    }

    if (!state.machines.length) {
        machineList.innerHTML = `<div class="machine-empty">${t("noOnline")}</div>`;
        return;
    }

    if (!machines.length) {
        machineList.innerHTML = `<div class="machine-empty">${t("noMatches")}</div>`;
    }
}

function getFilteredMachines() {
    const onlineMachines = state.machines.filter(machine => machine.status === "online");
    if (!state.machineSearch) {
        return onlineMachines;
    }

    return onlineMachines.filter(machine => {
        const haystack = [
            machine.machineId,
            machine.hostName,
            machine.ipAddress,
            machine.userName,
            machine.status
        ].join(" ").toLowerCase();

        return haystack.includes(state.machineSearch);
    });
}

function renderSupportRequests() {
    const requests = getFilteredSupportRequests();

    for (const request of requests) {
        const machine = state.machines.find(item => item.machineId === request.machineId);
        if (machine?.status !== "online") {
            continue;
        }

        const item = document.createElement("button");
        item.className = `machine${request.machineId === state.selectedMachineId ? " active" : ""}${request.unread ? " support-unread" : ""}`;
        item.type = "button";
        item.innerHTML = `
            <div class="machine-title">
                <span>${escapeHtml(request.hostName || request.machineId)}</span>
                <span class="machine-time">${formatTime(request.sentAtUtc)}</span>
            </div>
            <div class="machine-meta machine-message">
                ${escapeHtml(request.userName)} · ${escapeHtml(request.message)}
            </div>
            <div class="machine-meta">${escapeHtml(request.ipAddress)}</div>
        `;
        item.querySelector(".machine-message").textContent = `${request.userName} · ${request.message}`;
        item.querySelector(".machine-message").textContent = `${request.userName} - ${request.message}`;
        item.addEventListener("click", () => {
            markSupportRead(request.machineId);
            selectMachine(request.machineId);
        });
        machineList.appendChild(item);
    }

    if (!requests.length || !machineList.children.length) {
        machineList.innerHTML = `<div class="machine-empty">${t("noSupport")}</div>`;
    }
}

function getFilteredSupportRequests() {
    if (!state.machineSearch) {
        return state.supportRequests;
    }

    return state.supportRequests.filter(request => {
        const haystack = [
            request.machineId,
            request.hostName,
            request.ipAddress,
            request.userName,
            request.message
        ].join(" ").toLowerCase();

        return haystack.includes(state.machineSearch);
    });
}

function setSidebarTab(tab) {
    state.activeSidebarTab = tab;
    renderMachines();
}

function renderSidebarTabs() {
    supportTab.classList.toggle("active", state.activeSidebarTab === "support");
    onlineTab.classList.toggle("active", state.activeSidebarTab === "online");
    const unread = state.supportRequests.filter(item => item.unread).length;
    supportBadge.textContent = String(unread);
    supportBadge.classList.toggle("visible", unread > 0);
}

function applyLanguage() {
    document.documentElement.lang = language;
    document.title = t("appTitle");
    brandTitle.textContent = t("brandTitle");
    connectionStatus.textContent = t("disconnected");
    document.querySelector(".rail-button.active").textContent = "•";
    refreshButton.title = t("refresh");
    refreshButton.textContent = "↻";
    machineSearchInput.placeholder = t("searchPlaceholder");
    supportTabLabel.textContent = t("supportTitle");
    onlineTab.textContent = t("machinesTitle");
    selectedName.textContent = t("noMachineSelected");
    emptyScreen.textContent = t("emptySelect");
    chatTitle.textContent = t("chat");
    chatTarget.textContent = t("noTarget");
    chatInput.placeholder = t("chatPlaceholder");
    chatSendButton.textContent = t("send");
}

function markSupportRead(machineId) {
    state.supportRequests = state.supportRequests.map(request =>
        request.machineId === machineId ? { ...request, unread: false } : request);
    renderSidebarTabs();
}

function showSupportToast(request) {
    supportToast.innerHTML = `<strong>${escapeHtml(request.hostName || request.machineId)}</strong><br>${escapeHtml(request.message)}`;
    supportToast.classList.add("visible");
    clearTimeout(showSupportToast.timeoutId);
    showSupportToast.timeoutId = setTimeout(() => {
        supportToast.classList.remove("visible");
    }, 5000);
}

function selectMachine(machineId) {
    state.selectedMachineId = machineId;
    state.selectedMachine = state.machines.find(machine => machine.machineId === machineId) ?? null;
    state.remoteConnected = false;
    state.remoteControlEnabled = false;
    updateRemoteToggleButton();
    updateRemoteControlButton();
    updateLockButton();
    updateFullscreenButton();
    state.lastCursor = null;
    chatLog.innerHTML = "";
    screenImage.removeAttribute("src");
    screenStage.style.display = "none";
    screenImage.style.display = "none";
    remoteCursor.removeAttribute("src");
    remoteCursor.style.display = "none";
    emptyScreen.style.display = "block";
    emptyScreen.textContent = t("pressConnect");
    renderMachines();
    renderSelection();
}

function renderSelection() {
    if (!state.selectedMachine) {
        selectedName.textContent = t("noMachineSelected");
        selectedMeta.textContent = "";
        chatTarget.textContent = t("noTarget");
        updateRemoteToggleButton();
        updateRemoteControlButton();
        updateLockButton();
        return;
    }

    selectedName.textContent = state.selectedMachine.hostName || state.selectedMachine.machineId;
    selectedMeta.textContent = `${state.selectedMachine.ipAddress} · ${state.selectedMachine.userName}`;
    selectedMeta.textContent = `${state.selectedMachine.ipAddress} · ${state.selectedMachine.userName}`;
    selectedMeta.textContent = `${state.selectedMachine.ipAddress} · ${state.selectedMachine.userName}`;
    selectedMeta.textContent = `${state.selectedMachine.ipAddress} - ${state.selectedMachine.userName}`;
    chatTarget.textContent = state.selectedMachine.hostName || state.selectedMachine.machineId;
    updateRemoteToggleButton();
    updateRemoteControlButton();
    updateLockButton();
}

function updateRemoteToggleButton() {
    connectButton.disabled = !state.selectedMachineId;
    connectButton.classList.toggle("connected", state.remoteConnected);
    connectButton.textContent = state.remoteConnected ? t("disconnect") : t("connect");
    connectButton.title = state.remoteConnected ? t("disconnectTitle") : t("connectTitle");
}

function updateRemoteControlButton() {
    remoteControlButton.disabled = !state.remoteConnected || !state.selectedMachineId;
    remoteControlButton.classList.toggle("active", state.remoteControlEnabled);
    remoteControlButton.textContent = state.remoteControlEnabled ? t("viewOnly") : t("remote");
    remoteControlButton.title = state.remoteControlEnabled
        ? t("viewOnlyTitle")
        : t("remoteTitle");
}

function updateLockButton() {
    const locked = Boolean(state.selectedMachine?.mouseLocked);
    lockButton.disabled = !state.remoteConnected || !state.remoteControlEnabled || !state.selectedMachineId;
    lockButton.classList.toggle("locked", locked);
    lockButton.textContent = locked ? t("unlockMouse") : t("lockMouse");
    lockButton.title = state.remoteControlEnabled
        ? t("lockTitle")
        : t("lockDisabledTitle");
}

function updateFullscreenButton() {
    const active = document.fullscreenElement === remotePanel;
    fullscreenButton.disabled = !state.remoteConnected;
    fullscreenButton.classList.toggle("active", active);
    fullscreenButton.textContent = active ? t("exitFullscreen") : t("fullscreen");
}

function statusText(status) {
    if (status === "online") {
        return language === "vi" ? "đang chạy" : "online";
    }

    return status ?? "";
}

function buttonName(button) {
    if (button === 2) return "right";
    if (button === 1) return "middle";
    return "left";
}

function formatTime(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return "";
    }

    return date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
}

function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

start();
