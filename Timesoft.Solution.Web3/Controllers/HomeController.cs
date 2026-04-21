using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Timesoft.Solution.Web3.Models;
using Timesoft.Solution.Web3.Services;

namespace Timesoft.Solution.Web3.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            string configuredApiBaseUrl = ReadAppSetting("LeaveCalculation-ApiBaseUrl");
            string signalRProvider = ReadAppSetting("SignalR:Provider") ?? "Local";
            string apiBaseUrl = string.IsNullOrWhiteSpace(configuredApiBaseUrl)
                || string.Equals(configuredApiBaseUrl, "auto", StringComparison.OrdinalIgnoreCase)
                ? "https://localhost:5002"
                : configuredApiBaseUrl;

            var model = new LeaveCalculationPageViewModel
            {
                ApiBaseUrl = apiBaseUrl,
                HubUrl = ReadAppSetting("LeaveCalculation-HubUrl") ?? "https://localhost:5003/hubs/jobstatus",
                SignalREnabled = ReadBoolean("SignalR:Enabled", true),
                RestoreStorageMode = NormalizeRestoreStorageMode(ReadAppSetting("LeaveCalculation-RestoreStorage")),
                SignalRProvider = signalRProvider,
                CurrentYear = DateTime.Now.Year,
                Companies = BuildCompanies(),
                Departments = BuildDepartments(),
                Employees = BuildEmployees()
            };

            return View(model);
        }

        public ActionResult ViewLeave()
        {
            return View();
        }

        private static IReadOnlyList<SelectOption> BuildCompanies()
        {
            return new[]
            {
                new SelectOption("COMPANY_A", "Company A - TSTAFF"),
                new SelectOption("COMPANY_B", "Company B - CLIENT2"),
                new SelectOption("COMPANY_C", "Company C - SAAS")
            };
        }

        private static IReadOnlyList<SelectOption> BuildDepartments()
        {
            return new[]
            {
                new SelectOption("HR", "HUMAN RESOURCE DEPARTMENT"),
                new SelectOption("FIN", "FINANCE DEPARTMENT"),
                new SelectOption("IT", "IT DEPARTMENT"),
                new SelectOption("SALES", "SALES DEPARTMENT"),
                new SelectOption("PUR", "PURCHASING DEPARTMENT"),
                new SelectOption("SG", "SINGAPORE DIVISION")
            };
        }

        private static IReadOnlyList<SelectOption> BuildEmployees()
        {
            return new[]
            {
                new SelectOption("ALL", "All selected employees"),
                new SelectOption("001", "001 - ANDY LOW"),
                new SelectOption("002", "002 - BEN LIM"),
                new SelectOption("003", "003 - COLIN KOH"),
                new SelectOption("004", "004 - DAVID GAN"),
                new SelectOption("005", "005 - EUGENE ONG"),
                new SelectOption("006", "006 - FRASER PANG"),
                new SelectOption("101", "101 - ANGELA GOH"),
                new SelectOption("102", "102 - BETTY CHIA"),
                new SelectOption("103", "103 - CECILIA NG"),
                new SelectOption("104", "104 - DAPHNE TAN"),
                new SelectOption("105", "105 - EMILY WONG"),
                new SelectOption("106", "106 - FIONA WONG"),
                new SelectOption("801", "801 - RACHEL WONG"),
                new SelectOption("802", "802 - SUSAN TAY"),
                new SelectOption("803", "803 - TERESA TAN"),
                new SelectOption("804", "804 - UNICE CHENG"),
                new SelectOption("8040", "8040 - COPY UNICE CHENG"),
                new SelectOption("805", "805 - VIVIAN CHIA")
            };
        }

        private static string NormalizeRestoreStorageMode(string configuredMode)
        {
            if (string.Equals(configuredMode, "off", StringComparison.OrdinalIgnoreCase))
            {
                return "off";
            }

            if (string.Equals(configuredMode, "local", StringComparison.OrdinalIgnoreCase))
            {
                return "local";
            }

            return "session";
        }

        private static string ReadAppSetting(string key)
        {
            return AppSettings.Read(key);
        }

        private static bool ReadBoolean(string key, bool defaultValue)
        {
            bool value;

            return bool.TryParse(ReadAppSetting(key), out value)
                ? value
                : defaultValue;
        }
    }
}
