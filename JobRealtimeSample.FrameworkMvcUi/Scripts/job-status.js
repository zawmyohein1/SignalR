(function () {
    const app = document.getElementById("leaveCalculationApp");

    if (!app) {
        return;
    }

    const apiBaseUrl = app.dataset.apiBaseUrl;
    const hubUrl = app.dataset.hubUrl;
    const signalREnabled = app.dataset.signalrEnabled === "true";
    const seenNotifications = new Set();
    const demoLeaveTypeCode = "ANNU";
    const processingButtonHtml = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Processing...';

    let connection = null;
    let currentCalculationId = null;
    let currentHubAccessToken = null;
    let loginContext = null;
    let pollingTimer = null;

    const statusClasses = {
        Accepted: "text-bg-secondary",
        Started: "text-bg-primary",
        "Calculating leave entitlement": "text-bg-warning",
        Completed: "text-bg-success",
        Failed: "text-bg-danger"
    };

    const logStatusLabels = {
        "Calculating leave entitlement": "Calculating"
    };

    $("#companyCode").on("change", function () {
        const companyCode = $(this).val();
        const suffix = companyCode === "COMPANY_A" ? "A" : companyCode === "COMPANY_B" ? "B" : "C";
        $("#loginUserId").val(`HR_${suffix}`);
    });

    $("#loginButton").on("click", function () {
        const companyCode = $("#companyCode").val();
        const companyText = $("#companyCode option:selected").text();
        const loginUserId = ($("#loginUserId").val() || "").trim();
        const loginYear = $("#loginYear").val();
        const loginMonth = $("#loginMonth").val();

        if (!loginUserId) {
            alert("Login Id is required for the demo.");
            return;
        }

        loginContext = {
            companyCode,
            companyText,
            loginUserId,
            period: `${loginYear}/${loginMonth}`
        };

        $("#contextCompany").text(companyText);
        $("#contextUser").text(loginUserId);
        $("#contextPeriod").text(`Period ${loginContext.period}`);
        $("#loginPanel").addClass("d-none");
        $("#calculationPanel").removeClass("d-none");

        appendLog("Ready", "Login context selected. The next Process click will start Leave Calculation.", new Date());
    });

    $("#processButton").on("click", async function () {
        if (!loginContext) {
            alert("Please login first.");
            return;
        }

        const $button = $(this);
        $button.prop("disabled", true).html(processingButtonHtml);
        resetProcessUi();

        const requestStartedAt = performance.now();
        const payload = {
            companyCode: loginContext.companyCode,
            loginUserId: loginContext.loginUserId,
            departmentCode: $("#departmentCode").val(),
            employeeNo: $("#employeeNo").val(),
            leaveTypeCode: demoLeaveTypeCode,
            year: Number($("#calculationYear").val())
        };

        appendLog("HTTP request", "Posting Leave Calculation request. The browser will not wait for the full calculation to finish.", new Date());

        try {
            const response = await $.ajax({
                url: `${apiBaseUrl}/api/leave-calculations/start`,
                method: "POST",
                contentType: "application/json",
                data: JSON.stringify(payload)
            });

            const elapsedMilliseconds = Math.round(performance.now() - requestStartedAt);
            currentCalculationId = response.calculationId;
            currentHubAccessToken = response.hubAccessToken;

            $("#calculationIdDisplay").text(currentCalculationId);
            $("#apiResponseDisplay").text(`202 Accepted in ${elapsedMilliseconds} ms`);
            setStatus(response.status, response.message);
            appendLog(response.status, `${response.message} Calculation id returned immediately.`, new Date());

            if (canUseSignalR()) {
                await connectToHub(response);
                appendLog("SignalR", "Connected and joined the company/user/calculation group.", new Date());
            } else {
                useSnapshotPollingFallback();
                startPollingSnapshot(currentCalculationId);
            }

            await refreshCalculationSnapshot(currentCalculationId);
        } catch (error) {
            setStatus("Failed", "Could not start Leave Calculation.");
            appendLog("Failed", getAjaxErrorMessage(error), new Date());
            $button.prop("disabled", false).text("Process");
        }
    });

    async function connectToHub(response) {
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

        connection.on("LeaveCalculationStatusUpdated", function (notification) {
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

        await connection.start();
        setSignalRState("Connected", "text-bg-success");
        await joinCalculationGroup(response);
    }

    function canUseSignalR() {
        return signalREnabled && window.signalR;
    }

    function useSnapshotPollingFallback() {
        const message = signalREnabled
            ? "SignalR client script is not available. Snapshot polling is used instead."
            : "Realtime connection is disabled for this page. Snapshot polling is used instead.";

        setSignalRState(signalREnabled ? "Client not loaded" : "Disabled by page config", "text-bg-dark");
        appendLog("SignalR", message, new Date());
    }

    async function joinCalculationGroup(response) {
        const companyCode = response ? response.companyCode : loginContext.companyCode;
        const loginUserId = response ? response.loginUserId : loginContext.loginUserId;
        const calculationId = response ? response.calculationId : currentCalculationId;

        await connection.invoke("JoinCalculationGroup", companyCode, loginUserId, calculationId);
    }

    async function refreshCalculationSnapshot(calculationId) {
        const calculation = await $.getJSON(`${apiBaseUrl}/api/leave-calculations/${calculationId}`);

        setStatus(calculation.status, calculation.message);

        for (const item of calculation.history || []) {
            handleNotification(item);
        }

        if (calculation.status === "Completed" || calculation.status === "Failed") {
            finishProcess();
        }
    }

    function startPollingSnapshot(calculationId) {
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

    function handleNotification(notification) {
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

    function finishProcess() {
        stopPollingSnapshot();
        $("#processButton").prop("disabled", false).text("Process");
    }

    function resetProcessUi() {
        stopPollingSnapshot();
        seenNotifications.clear();
        currentCalculationId = null;
        currentHubAccessToken = null;
        $("#calculationIdDisplay").text("No calculation started");
        $("#apiResponseDisplay").text("Waiting for API response");
        setSignalRState(signalREnabled ? "Disconnected" : "Disabled by page config", "text-bg-secondary");
        $("#progressLog").empty();
        setStatus("Accepted", "Request has not been sent yet.");
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
