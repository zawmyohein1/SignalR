using System.Globalization;
using System.Xml.Linq;
using Timesoft.Solution.Api.Web4.Models;
using Timesoft.Solution.Api.Web4.Options;
using Microsoft.Extensions.Options;

namespace Timesoft.Solution.Api.Web4.Services;

public sealed class LeaveCalculationStore
{
    private static readonly object FileLock = new();
    private readonly string _xmlPath;

    public LeaveCalculationStore(IOptions<LeaveCalculationOptions> options, IHostEnvironment environment)
    {
        _xmlPath = ResolvePath(options.Value.XmlPath, environment.ContentRootPath);
    }

    public LeaveCalculationInfo Create(LeaveCalculationRequest request)
    {
        // XML file is the persistence store.
        var now = DateTimeOffset.UtcNow;
        var calculationId = Guid.NewGuid().ToString("N");
        const string message = "Leave entitlement process accepted and started in background.";

        var info = new LeaveCalculationInfo
        {
            CalculationId = calculationId,
            CompanyCode = Normalize(request.CompanyCode),
            LoginUserId = Normalize(request.LoginUserId),
            DepartmentCode = Normalize(request.DepartmentCode),
            EmployeeNo = NormalizeEmployee(request.EmployeeNo),
            Year = request.Year <= 0 ? DateTimeOffset.Now.Year : request.Year,
            Status = "Accepted",
            Message = message,
            CreatedAt = now,
            UpdatedAt = now
        };

        info.History.Add(CreateNotification(info, "Accepted", message, now));

        lock (FileLock)
        {
            var document = LoadDocument();
            document.Root!.Add(ToElement(info));
            SaveDocument(document);
        }

        return info.Snapshot();
    }

    public LeaveCalculationInfo? Get(string calculationId)
    {
        if (string.IsNullOrWhiteSpace(calculationId))
        {
            return null;
        }

        lock (FileLock)
        {
            var document = LoadDocument();
            var element = FindCalculation(document, calculationId);
            return element is null ? null : FromElement(element);
        }
    }

    public LeaveCalculationStatusNotification? UpdateStatus(string calculationId, string status, string message)
    {
        // Status update also appends one history row for UI replay.
        if (string.IsNullOrWhiteSpace(calculationId))
        {
            return null;
        }

        lock (FileLock)
        {
            var document = LoadDocument();
            var element = FindCalculation(document, calculationId);

            if (element is null)
            {
                return null;
            }

            var info = FromElement(element);
            var now = DateTimeOffset.UtcNow;
            var notification = CreateNotification(info, status, message, now);

            SetElementValue(element, "status", status);
            SetElementValue(element, "message", message);
            SetElementValue(element, "updatedAt", FormatDate(now));

            var history = element.Element("history");

            if (history is null)
            {
                history = new XElement("history");
                element.Add(history);
            }

            history.Add(ToHistoryElement(notification));
            SaveDocument(document);

            return notification;
        }
    }

    private static LeaveCalculationSummary ReadSummary(XElement element)
    {
        return new LeaveCalculationSummary
        {
            CalculationId = ReadString(element, "calculationId"),
            CompanyCode = ReadString(element, "companyCode"),
            LoginUserId = ReadString(element, "loginUserId"),
            DepartmentCode = ReadString(element, "departmentCode"),
            EmployeeNo = ReadString(element, "employeeNo"),
            Year = ReadInt(element, "year"),
            Status = ReadString(element, "status"),
            Message = ReadString(element, "message")
        };
    }

    private static T CopySummary<T>(T target, LeaveCalculationSummary source)
        where T : LeaveCalculationSummary
    {
        target.CalculationId = source.CalculationId;
        target.CompanyCode = source.CompanyCode;
        target.LoginUserId = source.LoginUserId;
        target.DepartmentCode = source.DepartmentCode;
        target.EmployeeNo = source.EmployeeNo;
        target.Year = source.Year;
        target.Status = source.Status;
        target.Message = source.Message;
        return target;
    }

    private static void WriteSummary(XElement element, LeaveCalculationSummary summary)
    {
        element.Add(
            new XElement("calculationId", summary.CalculationId),
            new XElement("companyCode", summary.CompanyCode),
            new XElement("loginUserId", summary.LoginUserId),
            new XElement("departmentCode", summary.DepartmentCode),
            new XElement("employeeNo", summary.EmployeeNo),
            new XElement("year", summary.Year.ToString(CultureInfo.InvariantCulture)),
            new XElement("status", summary.Status),
            new XElement("message", summary.Message));
    }

    private static XElement? FindCalculation(XDocument document, string calculationId)
    {
        return document.Root!
            .Elements("calculation")
            .FirstOrDefault(item => string.Equals(
                (string?)item.Element("calculationId"),
                calculationId,
                StringComparison.OrdinalIgnoreCase));
    }

    private static LeaveCalculationStatusNotification CreateNotification(
        LeaveCalculationSummary summary,
        string status,
        string message,
        DateTimeOffset timestamp)
    {
        var notification = CopySummary(new LeaveCalculationStatusNotification(), summary);
        notification.Status = status;
        notification.Message = message;
        notification.Timestamp = timestamp;
        return notification;
    }

    private XDocument LoadDocument()
    {
        EnsureFileExists();
        return XDocument.Load(_xmlPath);
    }

    private void SaveDocument(XDocument document)
    {
        EnsureFileExists();
        document.Save(_xmlPath);
    }

    private void EnsureFileExists()
    {
        // Create App_Data file on first run.
        var directory = Path.GetDirectoryName(_xmlPath);

        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(_xmlPath) || IsEmptyXmlFile(_xmlPath))
        {
            CreateEmptyDocument().Save(_xmlPath);
        }
    }

    private static bool IsEmptyXmlFile(string path)
    {
        var fileInfo = new FileInfo(path);

        if (fileInfo.Length == 0)
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(File.ReadAllText(path));
    }

    private static XDocument CreateEmptyDocument()
    {
        return new XDocument(new XElement("leaveCalculations"));
    }

    private static XElement ToElement(LeaveCalculationInfo info)
    {
        var element = new XElement("calculation");
        WriteSummary(element, info);
        element.Add(
            new XElement("createdAt", FormatDate(info.CreatedAt)),
            new XElement("updatedAt", FormatDate(info.UpdatedAt)),
            new XElement("history", info.History.Select(ToHistoryElement)));
        return element;
    }

    private static XElement ToHistoryElement(LeaveCalculationStatusNotification item)
    {
        var element = new XElement("entry");
        WriteSummary(element, item);
        element.Add(new XElement("timestamp", FormatDate(item.Timestamp)));
        return element;
    }

    private static LeaveCalculationInfo FromElement(XElement element)
    {
        var info = CopySummary(new LeaveCalculationInfo(), ReadSummary(element));
        info.CreatedAt = ReadDate(element, "createdAt");
        info.UpdatedAt = ReadDate(element, "updatedAt");

        var history = element.Element("history");

        if (history is not null)
        {
            info.History = history.Elements("entry").Select(FromHistoryElement).ToList();
        }

        return info;
    }

    private static LeaveCalculationStatusNotification FromHistoryElement(XElement element)
    {
        var notification = CopySummary(new LeaveCalculationStatusNotification(), ReadSummary(element));
        notification.Timestamp = ReadDate(element, "timestamp");
        return notification;
    }

    private static string ResolvePath(string configuredPath, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            configuredPath = "App_Data/LeaveCalculationJobs.xml";
        }

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(contentRootPath, configuredPath);
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
    }

    private static string NormalizeEmployee(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "ALL" : value.Trim().ToUpperInvariant();
    }

    private static void SetElementValue(XElement element, string name, string value)
    {
        var child = element.Element(name);

        if (child is null)
        {
            element.Add(new XElement(name, value));
        }
        else
        {
            child.Value = value;
        }
    }

    private static string ReadString(XElement element, string name)
    {
        return (string?)element.Element(name) ?? string.Empty;
    }

    private static int ReadInt(XElement element, string name)
    {
        return int.TryParse(ReadString(element, name), out var value) ? value : 0;
    }

    private static DateTimeOffset ReadDate(XElement element, string name)
    {
        return DateTimeOffset.TryParse(ReadString(element, name), out var value) ? value : DateTimeOffset.MinValue;
    }

    private static string FormatDate(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
    }
}
