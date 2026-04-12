(function () {
    const app = document.getElementById("leaveCalculationApp");

    if (!app) {
        return;
    }

    // Configuration
    const calculationProxyUrl = app.dataset.calculationProxyUrl || "/LeaveCalculations";
    const hubUrl = app.dataset.hubUrl;
    const signalREnabled = app.dataset.signalrEnabled === "true";
    const demoLeaveTypeCode = "ANNU";
    const synchronousExecutionMode = "SynchronousHttp";
    const processingButtonHtml = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Processing...';

    // Runtime state
    const seenNotifications = new Set();
    let connection = null;
    let currentCalculationId = null;
    let currentHubAccessToken = null;
    let loginContext = null;
    let pollingTimer = null;

    // Display mapping
    const statusClasses = {
        Accepted: "text-bg-secondary",
        Started: "text-bg-primary",
        "Calculating leave entitlement": "text-bg-warning",
        Completed: "text-bg-success",
        Failed: "text-bg-danger"
    };

    const logStatusLabels = {
        "Calculating leave entitlement": "Calculating",
        "HTTP response": "HTTP response"
    };

    bindEvents();

    // Event binding
    function bindEvents() {
        $("#companyCode").on("change", updateDemoLoginUser);
        $("#loginButton").on("click", handleLoginClick);
        $("#processButton").on("click", handleProcessClick);
    }

    // Login flow
    function updateDemoLoginUser() {
        // Demo login user changes with selected company.
        const companyCode = $("#companyCode").val();
        const suffix = companyCode === "COMPANY_A" ? "A" : companyCode === "COMPANY_B" ? "B" : "C";

        $("#loginUserId").val(`HR_${suffix}`);
    }

    function handleLoginClick() {
        // Save browser-side login context for this demo session.
        const loginUserId = ($("#loginUserId").val() || "").trim();

        if (!loginUserId) {
            alert("Login Id is required for the demo.");
            return;
        }

        loginContext = {
            companyCode: $("#companyCode").val(),
            companyText: $("#companyCode option:selected").text(),
            loginUserId,
            period: `${$("#loginYear").val()}/${$("#loginMonth").val()}`
        };

        showCalculationPanel();
        appendLog("Ready", "Login context selected. The next Process click will start Leave Calculation.", new Date());
    }

    function showCalculationPanel() {
        $("#contextCompany").text(loginContext.companyText);
        $("#contextUser").text(loginContext.loginUserId);
        $("#contextPeriod").text(`Period ${loginContext.period}`);
        $("#loginPanel").addClass("d-none");
        $("#calculationPanel").removeClass("d-none");
    }

    // Process start flow
    async function handleProcessClick() {
        // Start through MVC proxy; browser does not call API directly.
        if (!loginContext) {
            alert("Please login first.");
            return;
        }

        const $button = $("#processButton");
        const requestStartedAt = performance.now();

        $button.prop("disabled", true).html(processingButtonHtml);
        resetProcessUi();
        appendLog("HTTP request", "Posting Leave Calculation request. The browser will not wait for the full calculation to finish.", new Date());

        try {
            const response = await startCalculation(buildStartPayload());
            const elapsedMilliseconds = Math.round(performance.now() - requestStartedAt);

            await handleStartResponse(response, elapsedMilliseconds);
        } catch (error) {
            setStatus("Failed", "Could not start Leave Calculation.");
            appendLog("Failed", getAjaxErrorMessage(error), new Date());
            $button.prop("disabled", false).text("Process");
        }
    }

    function buildStartPayload() {
        return {
            companyCode: loginContext.companyCode,
            loginUserId: loginContext.loginUserId,
            departmentCode: $("#departmentCode").val(),
            employeeNo: $("#employeeNo").val(),
            leaveTypeCode: demoLeaveTypeCode,
            year: Number($("#calculationYear").val())
        };
    }

    function startCalculation(payload) {
        return $.ajax({
            url: `${calculationProxyUrl}/Start`,
            method: "POST",
            contentType: "application/json",
            data: JSON.stringify(payload)
        });
    }

    async function handleStartResponse(response, elapsedMilliseconds) {
        currentCalculationId = response.calculationId;
        currentHubAccessToken = response.hubAccessToken;

        // API tells the UI whether this was SignalR mode or normal HTTP mode.
        const isSynchronousHttp = response.executionMode === synchronousExecutionMode || response.signalREnabled === false;

        showStartResponse(response, elapsedMilliseconds, isSynchronousHttp);

        if (isSynchronousHttp) {
            await completeSynchronousHttpFlow(response, elapsedMilliseconds);
            return;
        }

        await startRealtimeOrFallback(response);
        await refreshCalculationSnapshot(currentCalculationId);
    }

    function showStartResponse(response, elapsedMilliseconds, isSynchronousHttp) {
        $("#calculationIdDisplay").text(currentCalculationId);
        $("#apiResponseDisplay").text(
            isSynchronousHttp
                ? `${response.status} in ${elapsedMilliseconds} ms`
                : `202 Accepted in ${elapsedMilliseconds} ms`);
        $("#executionModeDisplay").text(isSynchronousHttp ? "Normal HTTP request" : "Background + SignalR");

        setStatus(response.status, response.message);
        appendLog(
            response.status,
            isSynchronousHttp
                ? `${response.message} Normal HTTP request waited until the calculation finished.`
                : `${response.message} Calculation id returned immediately.`,
            new Date());
    }

    async function completeSynchronousHttpFlow(response, elapsedMilliseconds) {
        // No SignalR callback; load saved XML history after final API response.
        setSignalRState("Off", "text-bg-dark");
        $("#logModeHint").text("SignalR is disabled. Browser receives the final API response after the request completes.");

        await refreshCalculationSnapshot(currentCalculationId);
        appendLog(
            "HTTP response",
            `API returned ${response.status} after ${elapsedMilliseconds} ms. No realtime callback was used.`,
            new Date());
        finishProcess();
    }

    async function startRealtimeOrFallback(response) {
        if (canUseSignalR()) {
            // SignalR callback mode receives live updates by calculation group.
            await connectToHub(response);
            appendLog("SignalR", "Connected and joined the company/user/calculation group.", new Date());
            return;
        }

        // Safety fallback if the SignalR script is disabled or unavailable.
        useSnapshotPollingFallback();
        startPollingSnapshot(currentCalculationId);
    }

    // SignalR flow
    async function connectToHub(response) {
        // Recreate the connection for the current calculation id.
        if (connection) {
            await connection.stop();
        }

        connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, {
                accessTokenFactory: function () {
                    return currentHubAccessToken;
                }
            })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        registerHubHandlers();

        await connection.start();
        setSignalRState("Connected", "text-bg-success");
        await joinCalculationGroup(response);
    }

    function registerHubHandlers() {
        connection.on("LeaveCalculationStatusUpdated", function (notification) {
            // Hub pushes only messages for the joined company/user/calculation group.
            handleNotification(notification);
        });

        connection.onreconnecting(function () {
            setSignalRState("Reconnecting", "text-bg-warning");
        });

        connection.onreconnected(async function () {
            setSignalRState("Connected", "text-bg-success");

            if (currentCalculationId && loginContext) {
                await joinCalculationGroup();
                await refreshCalculationSnapshot(currentCalculationId);
            }
        });

        connection.onclose(function () {
            setSignalRState("Disconnected", "text-bg-secondary");
        });
    }

    function canUseSignalR() {
        return signalREnabled && window.signalR;
    }

    async function joinCalculationGroup(response) {
        // Group isolation prevents other browsers from receiving this calculation.
        const companyCode = response ? response.companyCode : loginContext.companyCode;
        const loginUserId = response ? response.loginUserId : loginContext.loginUserId;
        const calculationId = response ? response.calculationId : currentCalculationId;

        await connection.invoke("JoinCalculationGroup", companyCode, loginUserId, calculationId);
    }

    // Snapshot fallback flow
    async function refreshCalculationSnapshot(calculationId) {
        // Snapshot reads the XML-backed API state through MVC.
        const calculation = await $.getJSON(`${calculationProxyUrl}/Details/${encodeURIComponent(calculationId)}`);

        setStatus(calculation.status, calculation.message);

        for (const item of calculation.history || []) {
            handleNotification(item);
        }

        if (calculation.status === "Completed" || calculation.status === "Failed") {
            finishProcess();
        }
    }

    function useSnapshotPollingFallback() {
        const message = signalREnabled
            ? "SignalR client script is not available. Snapshot polling is used instead."
            : "Realtime connection is disabled for this page. Snapshot polling is used instead.";

        setSignalRState(signalREnabled ? "Client not loaded" : "Disabled by page config", "text-bg-dark");
        appendLog("SignalR", message, new Date());
    }

    function startPollingSnapshot(calculationId) {
        // Polling is fallback only; SignalR is preferred when enabled.
        stopPollingSnapshot();
        pollingTimer = window.setInterval(function () {
            refreshCalculationSnapshot(calculationId).catch(function () {
                appendLog("Failed", "Could not refresh calculation snapshot.", new Date());
            });
        }, 2000);
    }

    function stopPollingSnapshot() {
        if (pollingTimer) {
            window.clearInterval(pollingTimer);
            pollingTimer = null;
        }
    }

    // Notification handling
    function handleNotification(notification) {
        // Avoid duplicate rows from reconnects, polling, or snapshot refresh.
        const timestamp = notification.timestamp ? new Date(notification.timestamp) : new Date();
        const key = `${notification.calculationId}|${notification.status}|${notification.message}|${timestamp.toISOString()}`;

        if (seenNotifications.has(key)) {
            return;
        }

        seenNotifications.add(key);
        setStatus(notification.status, notification.message);
        appendLog(notification.status, notification.message, timestamp);

        if (notification.status === "Completed" || notification.status === "Failed") {
            finishProcess();
        }
    }

    // UI updates
    function resetProcessUi() {
        // Clear previous calculation before a new run.
        stopPollingSnapshot();
        seenNotifications.clear();
        currentCalculationId = null;
        currentHubAccessToken = null;

        $("#calculationIdDisplay").text("No calculation started");
        $("#apiResponseDisplay").text("Waiting for API response");
        $("#executionModeDisplay").text("Waiting for process");
        $("#logModeHint").text(
            signalREnabled
                ? "Only this browser group receives these updates."
                : "SignalR is disabled. Browser waits for the API response.");
        $("#progressLog").empty();

        setSignalRState(signalREnabled ? "Disconnected" : "Disabled by page config", "text-bg-secondary");
        setStatus("Accepted", "Request has not been sent yet.");
    }

    function finishProcess() {
        stopPollingSnapshot();
        $("#processButton").prop("disabled", false).text("Process");
    }

    function setStatus(status, message) {
        const badgeClass = statusClasses[status] || "text-bg-secondary";
        setBadge($("#currentStatusDisplay"), status, badgeClass);

        if (message) {
            $("#currentStatusDisplay").attr("title", message);
        }
    }

    function setSignalRState(text, badgeClass) {
        setBadge($("#signalrState"), text, badgeClass);
    }

    function setBadge($element, text, badgeClass) {
        $element
            .removeClass()
            .addClass(`badge ${badgeClass}`)
            .text(text);
    }

    function appendLog(status, message, timestamp) {
        // Keep newest progress visible.
        const timeText = timestamp.toLocaleTimeString();
        const badgeClass = statusClasses[status] || "text-bg-dark";
        const displayStatus = logStatusLabels[status] || status;
        const entry = $(`
            <div class="job-log-entry">
                <div class="log-meta">
                    <span class="badge ${badgeClass}">${escapeHtml(displayStatus)}</span>
                    <time>${escapeHtml(timeText)}</time>
                </div>
                <p>${escapeHtml(message)}</p>
            </div>
        `);

        $("#progressLog").append(entry);

        const log = $("#progressLog")[0];
        log.scrollTop = log.scrollHeight;
    }

    // Helpers
    function getAjaxErrorMessage(error) {
        if (error && error.responseJSON && error.responseJSON.message) {
            return error.responseJSON.message;
        }

        if (error && error.responseText) {
            return error.responseText;
        }

        if (error && error.statusText) {
            return `Request failed: ${error.statusText}`;
        }

        return "Request failed. Make sure the API and RealtimeHub projects are running.";
    }

    function escapeHtml(value) {
        return $("<div>").text(value || "").html();
    }
})();
