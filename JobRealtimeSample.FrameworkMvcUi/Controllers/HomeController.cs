using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using JobRealtimeSample.FrameworkMvcUi.Models;

namespace JobRealtimeSample.FrameworkMvcUi.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            string configuredApiBaseUrl = ConfigurationManager.AppSettings["LeaveCalculationApiBaseUrl"];
            string apiBaseUrl = string.IsNullOrWhiteSpace(configuredApiBaseUrl)
                || string.Equals(configuredApiBaseUrl, "auto", StringComparison.OrdinalIgnoreCase)
                    ? $"{Request.Url.Scheme}://localhost:5002"
                    : configuredApiBaseUrl;

            var model = new LeaveCalculationPageViewModel
            {
                ApiBaseUrl = apiBaseUrl,
                HubUrl = ConfigurationManager.AppSettings["RealtimeHubUrl"] ?? "https://localhost:5003/hubs/jobstatus",
                SignalREnabled = !string.Equals(
                    ConfigurationManager.AppSettings["SignalREnabled"],
                    "false",
                    StringComparison.OrdinalIgnoreCase),
                CurrentYear = DateTime.Now.Year,
                Companies = BuildCompanies(),
                Departments = BuildDepartments(),
                Employees = BuildEmployees(),
                LeaveTypes = BuildLeaveTypes()
            };

            return View(model);
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

        private static IReadOnlyList<DemoOption> BuildLeaveTypes()
        {
            return new[]
            {
                new DemoOption("ANNU", "ANNU - ANNUAL LEAVE"),
                new DemoOption("SICK", "SICK - SICK LEAVE"),
                new DemoOption("HOSP", "HOSP - HOSPITALISATION"),
                new DemoOption("CHILDLVE", "CHILDLVE - CHILD CARE LEAVE"),
                new DemoOption("COMP", "COMP - COMPASSIONATE LEAVE"),
                new DemoOption("EXAM", "EXAM - EXAM LEAVE"),
                new DemoOption("MATE", "MATE - MATERNITY LEAVE"),
                new DemoOption("PATE", "PATE - PATERNITY LEAVE"),
                new DemoOption("NPL", "NPL - NO PAY LEAVE"),
                new DemoOption("RO", "RO - REPLACEMENT OFF"),
                new DemoOption("SEMINAR", "SEMINAR - SEMINAR"),
                new DemoOption("TRAINING", "TRAINING - TRAINING LEAVE")
            };
        }
    }
}
