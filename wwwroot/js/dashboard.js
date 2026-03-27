// History buffers for sparklines (20 readings per sensor)
const history = {
    Temperature: [], Humidity: [], Pressure: []
};

const MAX_HISTORY = 20;

// ── SignalR connection (Observer pattern: this client observes the hub) ──────

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/sensorHub")
    .withAutomaticReconnect()
    .build();

connection.onreconnecting(() => setStatus(false));
connection.onreconnected(() => setStatus(true));

// Called once on connect — fills dashboard immediately from server snapshot
connection.on("ReceiveSnapshot", (snapshot) => {
    const fields = ["Temperature", "Humidity", "Pressure", "Pitch", "Roll", "Yaw"];
    fields.forEach(name => {
        const reading = snapshot[name.toLowerCase()];
        if (reading) applyReading(reading);
    });
});

// Called every ~1s as new readings arrive
connection.on("ReceiveReading", (reading) => {
    applyReading(reading);
});

// Alert events from the AlertDecorator
connection.on("ReceiveAlert", (alert) => {
    addAlert(alert);
    flashCard(alert.sensorName, alert.severity);
});

connection.start()
    .then(() => setStatus(true))
    .catch(err => console.error("SignalR error:", err));

// ── DOM helpers ───────────────────────────────────────────────────────────────

function applyReading(reading) {
    const name = reading.sensorName;
    const el = document.getElementById(`val-${name.toLowerCase()}`);
    if (!el) return;

    el.textContent = reading.value.toFixed(2);

    if (history[name] !== undefined) {
        history[name].push(reading.value);
        if (history[name].length > MAX_HISTORY) history[name].shift();
        updateSparkline(name, history[name]);
    }
}

function updateSparkline(name, values) {
    const container = document.getElementById(`spark-${name.toLowerCase()}`);
    if (!container) return;

    const min = Math.min(...values);
    const max = Math.max(...values);
    const range = max - min || 1;

    container.innerHTML = values.map(v => {
        const pct = Math.max(4, Math.round(((v - min) / range) * 100));
        return `<div class="bar" style="height:${pct}%"></div>`;
    }).join('');
}

function addAlert(alert) {
    const list = document.getElementById("alert-list");
    const li = document.createElement("li");
    li.className = alert.severity.toLowerCase() === "critical" ? "critical" : "";
    const time = new Date(alert.timestamp).toLocaleTimeString();
    li.textContent = `[${time}] ${alert.message}`;
    list.prepend(li);

    // Keep list trimmed to 30 entries
    while (list.children.length > 30) list.lastChild.remove();
}

function flashCard(sensorName, severity) {
    const card = document.getElementById(`card-${sensorName.toLowerCase()}`);
    if (!card) return;
    const cls = severity.toLowerCase() === "critical" ? "alert-critical" : "alert-warn";
    card.classList.add(cls);
    setTimeout(() => card.classList.remove(cls), 3000);
}

function setStatus(connected) {
    const el = document.getElementById("connection-status");
    el.textContent = connected ? "Live" : "Reconnecting…";
    el.className = `status ${connected ? "connected" : "disconnected"}`;
}
