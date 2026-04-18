(function (window, $) {
    "use strict";

    // Runtime state.
    const state = {
        config: null,
        connection: null,
        pollingTimer: null,
        seenNotifications: new Set(),
        currentCalculationId: null,
        currentHubAccessToken: null,
        currentCompanyCode: null,
        currentLoginUserId: null
    };

    function configure(options) {
        // Save the page config once.
        state.config = {
            calculationProxyUrl: options.calculationProxyUrl,
            hubUrl: options.hubUrl,
            signalRProvider: options.signalRProvider || "Local",
            signalREnabled: options.signalREnabled,
            callbacks: options.callbacks || {}
        };

        setSignalRProvider(state.config.signalRProvider);
    }

    async function reset() {
        // Clear the current job cycle.
        stopPollingSnapshot();
        await stopConnection();

        state.seenNotifications.clear();
        state.currentCalculationId = null;
        state.currentHubAccessToken = null;
        state.currentCompanyCode = null;
        state.currentLoginUserId = null;
    }

    async function start(response) {
        ensureConfigured();

        // Start a fresh job cycle.
        state.seenNotifications.clear();
        stopPollingSnapshot();
        await stopConnection();

        state.currentCalculationId = response.calculationId;
        state.currentHubAccessToken = response.hubAccessToken;
        state.currentCompanyCode = response.companyCode;
        state.currentLoginUserId = response.loginUserId;

        if (canUseSignalR()) {
            try {
                await connectToHub(response);
                notifyLog("SignalR", "Connected and joined the company/user/calculation group.", new Date());
                return;
            } catch (error) {
                notifySignalRState("Polling", "text-bg-warning");
                notifyLog("SignalR", "Could not connect to SignalR. Snapshot polling is used instead.", new Date());
            }
        } else {
            useSnapshotPollingFallback();
        }

        startPollingSnapshot(state.currentCalculationId);
    }

    async function resume(savedCalculation) {
        ensureConfigured();

        stopPollingSnapshot();
        await stopConnection();

        state.currentCalculationId = savedCalculation.calculationId;
        state.currentHubAccessToken = savedCalculation.hubAccessToken;
        state.currentCompanyCode = savedCalculation.companyCode;
        state.currentLoginUserId = savedCalculation.loginUserId;

        if (canUseSignalR() && state.currentHubAccessToken) {
            try {
                await connectToHub(savedCalculation);
                notifyLog("SignalR", "Reconnected to the saved calculation group.", new Date());
                return;
            } catch (error) {
                notifySignalRState("Polling", "text-bg-warning");
                notifyLog("SignalR", "Could not reconnect to SignalR. Snapshot polling is used instead.", new Date());
            }
        } else {
            useSnapshotPollingFallback();
        }

        startPollingSnapshot(state.currentCalculationId);
    }

    async function refreshSnapshot(calculationId) {
        ensureConfigured();

        const url = `${state.config.calculationProxyUrl}/Details/${encodeURIComponent(calculationId)}`;
        const calculation = await $.getJSON(url);

        notifyStatus(calculation.status, calculation.message);

        for (const item of calculation.history || []) {
            handleNotification(item);
        }

        if (calculation.status === "Completed" || calculation.status === "Failed") {
            notifyFinish();
        }

        return calculation;
    }

    async function connectToHub(response) {
        // Open the SignalR connection for this job.
        state.connection = new signalR.HubConnectionBuilder()
            .withUrl(state.config.hubUrl, {
                accessTokenFactory: function () {
                    return state.currentHubAccessToken;
                }
            })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        registerHubHandlers();

        await state.connection.start();
        notifySignalRState("Connected", "text-bg-success");
        await joinCalculationGroup(response);
    }

    function registerHubHandlers() {
        // Keep the UI in sync with hub events.
        state.connection.on("LeaveCalculationStatusUpdated", function (notification) {
            handleNotification(notification);
        });

        state.connection.onreconnecting(function () {
            notifySignalRState("Reconnecting", "text-bg-warning");
        });

        state.connection.onreconnected(async function () {
            notifySignalRState("Connected", "text-bg-success");

            if (state.currentCalculationId) {
                await joinCalculationGroup({
                    companyCode: state.currentCompanyCode,
                    loginUserId: state.currentLoginUserId,
                    calculationId: state.currentCalculationId
                });

                await refreshSnapshot(state.currentCalculationId);
            }
        });

        state.connection.onclose(function () {
            notifySignalRState("Disconnected", "text-bg-secondary");
        });
    }

    function canUseSignalR() {
        return state.config.signalREnabled && window.signalR;
    }

    async function joinCalculationGroup(response) {
        await state.connection.invoke(
            "JoinCalculationGroup",
            response.companyCode,
            response.loginUserId,
            response.calculationId,
            state.currentHubAccessToken);
    }

    function useSnapshotPollingFallback() {
        // Fall back to polling when live push is not available.
        const message = state.config.signalREnabled
            ? "SignalR client script is not available. Snapshot polling is used instead."
            : "Realtime connection is disabled for this page. Snapshot polling is used instead.";

        notifySignalRState(
            state.config.signalREnabled ? "Client not loaded" : "Disabled by page config",
            "text-bg-dark");
        notifyLog("SignalR", message, new Date());
    }

    function startPollingSnapshot(calculationId) {
        stopPollingSnapshot();

        state.pollingTimer = window.setInterval(function () {
            refreshSnapshot(calculationId).catch(function () {
                notifyLog("Failed", "Could not refresh calculation snapshot.", new Date());
            });
        }, 2000);
    }

    function stopPollingSnapshot() {
        if (state.pollingTimer) {
            window.clearInterval(state.pollingTimer);
            state.pollingTimer = null;
        }
    }

    async function stopConnection() {
        if (!state.connection) {
            return;
        }

        const connection = state.connection;
        state.connection = null;
        await connection.stop();
    }

    function handleNotification(notification) {
        // Skip duplicate updates.
        const timestamp = notification.timestamp ? new Date(notification.timestamp) : new Date();
        const key = `${notification.calculationId}|${notification.status}|${notification.message}|${timestamp.toISOString()}`;

        if (state.seenNotifications.has(key)) {
            return;
        }

        state.seenNotifications.add(key);
        notifyStatus(notification.status, notification.message);
        notifyLog(notification.status, notification.message, timestamp);

        if (notification.status === "Completed" || notification.status === "Failed") {
            notifyFinish();
        }
    }

    function ensureConfigured() {
        if (!state.config) {
            throw new Error("SignalR client is not configured.");
        }
    }

    function notifySignalRState(text, badgeClass) {
        invokeCallback("onSignalRState", text, badgeClass);
    }

    function setSignalRProvider(provider) {
        const normalizedProvider = (provider || "Local").toString();
        $("#signalrProviderDisplay")
            .removeClass("text-bg-secondary text-bg-success text-bg-primary text-bg-dark signalr-provider-local signalr-provider-azure signalr-provider-disabled")
            .addClass(`signalr-provider-${normalizedProvider.toLowerCase()}`)
            .text(normalizedProvider);
    }

    function notifyStatus(status, message) {
        invokeCallback("onStatus", status, message);
    }

    function notifyLog(status, message, timestamp) {
        invokeCallback("onLog", status, message, timestamp);
    }

    function notifyFinish() {
        invokeCallback("onFinish");
    }

    function invokeCallback(name) {
        const callback = state.config && state.config.callbacks && state.config.callbacks[name];
        if (typeof callback !== "function") {
            return;
        }

        const args = Array.prototype.slice.call(arguments, 1);
        callback.apply(null, args);
    }

    window.leaveCalculationSignalRClient = {
        configure: configure,
        reset: reset,
        start: start,
        resume: resume,
        refreshSnapshot: refreshSnapshot
    };
})(window, jQuery);
