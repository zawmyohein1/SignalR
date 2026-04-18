using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JobRealtimeSample.MvcUi.Models;

namespace JobRealtimeSample.MvcUi.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _configuration;

    public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        var signalRProvider = _configuration["LeaveCalculationDemo:SignalRProvider"] ?? "Local";
        var signalREnabled = _configuration.GetValue("LeaveCalculationDemo:SignalREnabled", true)
            && !string.Equals(signalRProvider, "Disabled", StringComparison.OrdinalIgnoreCase);

        var model = new LeaveCalculationPageViewModel
        {
            ApiBaseUrl = _configuration["LeaveCalculationDemo:ApiBaseUrl"] ?? "https://localhost:5102",
            HubUrl = _configuration["LeaveCalculationDemo:HubUrl"] ?? "https://localhost:5003/hubs/jobstatus",
            SignalREnabled = signalREnabled,
            RestoreStorageMode = NormalizeRestoreStorageMode(_configuration["LeaveCalculationDemo:RestoreStorage"]),
            SignalRProvider = signalRProvider,
            CurrentYear = DateTime.Now.Year,
            Companies = BuildCompanies(),
            Departments = BuildDepartments(),
            Employees = BuildEmployees()
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult ViewLeave()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static IReadOnlyList<DemoOption> BuildCompanies()
    {
        return
        [
            new("COMPANY_A", "Company A - TSTAFF demo"),
            new("COMPANY_B", "Company B - CLIENT2 demo"),
            new("COMPANY_C", "Company C - SAAS demo")
        ];
    }

    private static IReadOnlyList<DemoOption> BuildDepartments()
    {
        return
        [
            new("HR", "HUMAN RESOURCE DEPARTMENT"),
            new("FIN", "FINANCE DEPARTMENT"),
            new("IT", "IT DEPARTMENT"),
            new("SALES", "SALES DEPARTMENT"),
            new("PUR", "PURCHASING DEPARTMENT"),
            new("SG", "SINGAPORE DIVISION")
        ];
    }

    private static IReadOnlyList<DemoOption> BuildEmployees()
    {
        return
        [
            new("ALL", "All selected employees"),
            new("001", "001 - ANDY LOW"),
            new("002", "002 - BEN LIM"),
            new("003", "003 - COLIN KOH"),
            new("004", "004 - DAVID GAN"),
            new("005", "005 - EUGENE ONG"),
            new("006", "006 - FRASER PANG"),
            new("101", "101 - ANGELA GOH"),
            new("102", "102 - BETTY CHIA"),
            new("103", "103 - CECILIA NG"),
            new("104", "104 - DAPHNE TAN"),
            new("105", "105 - EMILY WONG"),
            new("106", "106 - FIONA WONG"),
            new("801", "801 - RACHEL WONG"),
            new("802", "802 - SUSAN TAY"),
            new("803", "803 - TERESA TAN"),
            new("804", "804 - UNICE CHENG"),
            new("8040", "8040 - COPY UNICE CHENG"),
            new("805", "805 - VIVIAN CHIA")
        ];
    }

    private static string NormalizeRestoreStorageMode(string? configuredMode)
    {
        return configuredMode?.Trim().ToLowerInvariant() switch
        {
            "off" => "off",
            "local" => "local",
            _ => "session"
        };
    }

}
