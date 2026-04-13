using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using Timesoft.Solution.Web3.Models;

namespace Timesoft.Solution.Web3.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            string configuredApiBaseUrl = ReadAppSetting(
                "LeaveCalculationDemo-ApiBaseUrl",
                "LeaveCalculationApiBaseUrl");
            string apiBaseUrl = string.IsNullOrWhiteSpace(configuredApiBaseUrl)
                || string.Equals(configuredApiBaseUrl, "auto", StringComparison.OrdinalIgnoreCase)
                    ? $"{Request.Url.Scheme}://localhost:5002"
                    : configuredApiBaseUrl;

            var model = new LeaveCalculationPageViewModel
            {
                ApiBaseUrl = apiBaseUrl,
                HubUrl = ReadAppSetting(
                    "LeaveCalculationDemo-HubUrl",
                    "RealtimeHubUrl") ?? "https://localhost:5003/hubs/jobstatus",
                SignalREnabled = !string.Equals(
                    ReadAppSetting(
                        "LeaveCalculationDemo-SignalREnabled",
                        "SignalREnabled"),
                    "false",
                    StringComparison.OrdinalIgnoreCase),
                RestoreStorageMode = NormalizeRestoreStorageMode(ReadAppSetting(
                    "LeaveCalculationDemo-RestoreStorage",
                    "LeaveCalculationRestoreStorage")),
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

        private static IReadOnlyList<DemoOption> BuildCompanies()
        {
            return new[]
            {
                new DemoOption("COMPANY_A", "Company A - TSTAFF demo"),
                new DemoOption("COMPANY_B", "Company B - CLIENT2 demo"),
                new DemoOption("COMPANY_C", "Company C - SAAS demo")
            };
        }

        private static IReadOnlyList<DemoOption> BuildDepartments()
        {
            return new[]
            {
                new DemoOption("HR", "HUMAN RESOURCE DEPARTMENT"),
                new DemoOption("FIN", "FINANCE DEPARTMENT"),
                new DemoOption("IT", "IT DEPARTMENT"),
                new DemoOption("SALES", "SALES DEPARTMENT"),
                new DemoOption("PUR", "PURCHASING DEPARTMENT"),
                new DemoOption("SG", "SINGAPORE DIVISION")
            };
        }

        private static IReadOnlyList<DemoOption> BuildEmployees()
        {
            return new[]
            {
                new DemoOption("ALL", "All selected employees"),
                new DemoOption("001", "001 - ANDY LOW"),
                new DemoOption("002", "002 - BEN LIM"),
                new DemoOption("003", "003 - COLIN KOH"),
                new DemoOption("004", "004 - DAVID GAN"),
                new DemoOption("005", "005 - EUGENE ONG"),
                new DemoOption("006", "006 - FRASER PANG"),
                new DemoOption("101", "101 - ANGELA GOH"),
                new DemoOption("102", "102 - BETTY CHIA"),
                new DemoOption("103", "103 - CECILIA NG"),
                new DemoOption("104", "104 - DAPHNE TAN"),
                new DemoOption("105", "105 - EMILY WONG"),
                new DemoOption("106", "106 - FIONA WONG"),
                new DemoOption("801", "801 - RACHEL WONG"),
                new DemoOption("802", "802 - SUSAN TAY"),
                new DemoOption("803", "803 - TERESA TAN"),
                new DemoOption("804", "804 - UNICE CHENG"),
                new DemoOption("8040", "8040 - COPY UNICE CHENG"),
                new DemoOption("805", "805 - VIVIAN CHIA")
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

        private static string ReadAppSetting(string primaryKey, string fallbackKey)
        {
            string value = ConfigurationManager.AppSettings[primaryKey];

            return string.IsNullOrWhiteSpace(value)
                ? ConfigurationManager.AppSettings[fallbackKey]
                : value;
        }

    }
}
