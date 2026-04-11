(function () {
    const app = document.getElementById("leaveCalculationApp");

    if (!app) {
        return;
    }

    const apiBaseUrl = app.dataset.apiBaseUrl;
    const hubUrl = app.dataset.hubUrl;
    const signalREnabled = app.dataset.signalrEnabled === "true";
    const seenNotifications = new Set();

    let connection = null;
    let currentCalculationId = null;
    let currentHubAccessToken = null;
    let loginContext = null;
    let pollingTimer = null;

    const statusClasses = {
        Accepted: "text-bg-secondary",
        Started: "text-bg-primary",
        "Loading selected employees": "text-bg-info",
        "Calculating leave entitlement": "text-bg-warning",
        "Updating leave balances": "text-bg-warning",
        Completed: "text-bg-success",
        Failed: "text-bg-danger"
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
        $button.prop("disabled", true).text("Processing...");
        resetProcessUi();

        const requestStartedAt = performance.now();
        const payload = {
            companyCode: loginContext.companyCode,
            loginUserId: loginContext.loginUserId,
            departmentCode: $("#departmentCode").val(),
            employeeNo: $("#employeeNo").val(),
            leaveTypeCode: $("#leaveTypeCode").val(),
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

            if (signalREnabled) {
                await connectToHub(response);
                appendLog("SignalR", "Connected and joined the company/user/calculation group.", new Date());
            } else {
                $("#signalrState").removeClass().addClass("badge text-bg-dark").text("Disabled by page config");
                appendLog("SignalR", "Realtime connection is disabled for this page. Snapshot polling is used instead.", new Date());
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
            $("#signalrState").removeClass().addClass("badge text-bg-warning").text("Reconnecting");
        });

        connection.onreconnected(async function () {
            $("#signalrState").removeClass().addClass("badge text-bg-success").text("Connected");

            if (currentCalculationId && loginContext) {
                await joinCalculationGroup();
                await refreshCalculationSnapshot(currentCalculationId);
            }
        });

        connection.onclose(function () {
            $("#signalrState").removeClass().addClass("badge text-bg-secondary").text("Disconnected");
        });

        await connection.start();
        $("#signalrState").removeClass().addClass("badge text-bg-success").text("Connected");
        await joinCalculationGroup(response);
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
        $("#signalrState").removeClass().addClass("badge text-bg-secondary").text(signalREnabled ? "Disconnected" : "Disabled by page config");
        $("#progressLog").empty();
        setStatus("Accepted", "Request has not been sent yet.");
    }

    function setStatus(status, message) {
        const badgeClass = statusClasses[status] || "text-bg-secondary";
        $("#currentStatusDisplay")
            .removeClass()
            .addClass(`badge ${badgeClass}`)
            .text(status);

        if (message) {
            $("#currentStatusDisplay").attr("title", message);
        }
    }

    function appendLog(status, message, timestamp) {
        const timeText = timestamp.toLocaleTimeString();
        const badgeClass = statusClasses[status] || "text-bg-dark";
        const entry = $(`
            <div class="job-log-entry">
                <div class="log-meta">
                    <span class="badge ${badgeClass}">${escapeHtml(status)}</span>
                    <time>${escapeHtml(timeText)}</time>
                </div>
                <p>${escapeHtml(message)}</p>
            </div>
        `);

        $("#progressLog").append(entry);
        entry[0].scrollIntoView({ block: "nearest" });
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

