using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Xml.Linq;
using Timesoft.Solution.Api.Web3.Models;

namespace Timesoft.Solution.Api.Web3.Services
{
    public sealed class XmlLeaveCalculationStore
    {
        private static readonly object FileLock = new object();
        private readonly string _xmlPath;

        public XmlLeaveCalculationStore()
        {
            string configuredPath = ConfigurationManager.AppSettings["LeaveCalculationXmlPath"];
            _xmlPath = ResolvePath(configuredPath);
        }

        public LeaveCalculationInfo Create(LeaveCalculationStartRequest request)
        {
            // XML file is the demo persistence store.
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string calculationId = Guid.NewGuid().ToString("N");
            string message = "Leave entitlement process accepted and started in background.";

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
                XDocument document = LoadDocument();
                document.Root.Add(ToElement(info));
                SaveDocument(document);
            }

            return info.Snapshot();
        }

        public LeaveCalculationInfo Get(string calculationId)
        {
            if (string.IsNullOrWhiteSpace(calculationId))
            {
                return null;
            }

            lock (FileLock)
            {
                XDocument document = LoadDocument();
                XElement element = FindCalculation(document, calculationId);
                return element == null ? null : FromElement(element);
            }
        }

        public LeaveCalculationStatusNotification UpdateStatus(string calculationId, string status, string message)
        {
            // Status update also appends one history row for UI replay.
            if (string.IsNullOrWhiteSpace(calculationId))
            {
                return null;
            }

            lock (FileLock)
            {
                XDocument document = LoadDocument();
                XElement element = FindCalculation(document, calculationId);

                if (element == null)
                {
                    return null;
                }

                LeaveCalculationInfo info = FromElement(element);
                DateTimeOffset now = DateTimeOffset.UtcNow;
                LeaveCalculationStatusNotification notification = CreateNotification(info, status, message, now);

                SetElementValue(element, "status", status);
                SetElementValue(element, "message", message);
                SetElementValue(element, "updatedAt", FormatDate(now));

                XElement history = element.Element("history");

                if (history == null)
                {
                    history = new XElement("history");
                    element.Add(history);
                }

                history.Add(ToHistoryElement(notification));
                SaveDocument(document);

                return notification;
            }
        }

        private static XElement FindCalculation(XDocument document, string calculationId)
        {
            return document.Root
                .Elements("calculation")
                .FirstOrDefault(item => string.Equals(
                    (string)item.Element("calculationId"),
                    calculationId,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static LeaveCalculationStatusNotification CreateNotification(
            LeaveCalculationInfo info,
            string status,
            string message,
            DateTimeOffset timestamp)
        {
            return new LeaveCalculationStatusNotification
            {
                CalculationId = info.CalculationId,
                CompanyCode = info.CompanyCode,
                LoginUserId = info.LoginUserId,
                DepartmentCode = info.DepartmentCode,
                EmployeeNo = info.EmployeeNo,
                Year = info.Year,
                Status = status,
                Message = message,
                Timestamp = timestamp
            };
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
            string directory = Path.GetDirectoryName(_xmlPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(_xmlPath))
            {
                new XDocument(new XElement("leaveCalculations")).Save(_xmlPath);
            }
        }

        private static XElement ToElement(LeaveCalculationInfo info)
        {
            return new XElement(
                "calculation",
                new XElement("calculationId", info.CalculationId),
                new XElement("companyCode", info.CompanyCode),
                new XElement("loginUserId", info.LoginUserId),
                new XElement("departmentCode", info.DepartmentCode),
                new XElement("employeeNo", info.EmployeeNo),
                new XElement("year", info.Year.ToString(CultureInfo.InvariantCulture)),
                new XElement("status", info.Status),
                new XElement("message", info.Message),
                new XElement("createdAt", FormatDate(info.CreatedAt)),
                new XElement("updatedAt", FormatDate(info.UpdatedAt)),
                new XElement("history", info.History.Select(ToHistoryElement)));
        }

        private static XElement ToHistoryElement(LeaveCalculationStatusNotification item)
        {
            return new XElement(
                "entry",
                new XElement("calculationId", item.CalculationId),
                new XElement("companyCode", item.CompanyCode),
                new XElement("loginUserId", item.LoginUserId),
                new XElement("departmentCode", item.DepartmentCode),
                new XElement("employeeNo", item.EmployeeNo),
                new XElement("year", item.Year.ToString(CultureInfo.InvariantCulture)),
                new XElement("status", item.Status),
                new XElement("message", item.Message),
                new XElement("timestamp", FormatDate(item.Timestamp)));
        }

        private static LeaveCalculationInfo FromElement(XElement element)
        {
            var info = new LeaveCalculationInfo
            {
                CalculationId = ReadString(element, "calculationId"),
                CompanyCode = ReadString(element, "companyCode"),
                LoginUserId = ReadString(element, "loginUserId"),
                DepartmentCode = ReadString(element, "departmentCode"),
                EmployeeNo = ReadString(element, "employeeNo"),
                Year = ReadInt(element, "year"),
                Status = ReadString(element, "status"),
                Message = ReadString(element, "message"),
                CreatedAt = ReadDate(element, "createdAt"),
                UpdatedAt = ReadDate(element, "updatedAt")
            };

            XElement history = element.Element("history");

            if (history != null)
            {
                info.History = history.Elements("entry").Select(FromHistoryElement).ToList();
            }

            return info;
        }

        private static LeaveCalculationStatusNotification FromHistoryElement(XElement element)
        {
            return new LeaveCalculationStatusNotification
            {
                CalculationId = ReadString(element, "calculationId"),
                CompanyCode = ReadString(element, "companyCode"),
                LoginUserId = ReadString(element, "loginUserId"),
                DepartmentCode = ReadString(element, "departmentCode"),
                EmployeeNo = ReadString(element, "employeeNo"),
                Year = ReadInt(element, "year"),
                Status = ReadString(element, "status"),
                Message = ReadString(element, "message"),
                Timestamp = ReadDate(element, "timestamp")
            };
        }

        private static string ResolvePath(string configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                configuredPath = "~/App_Data/LeaveCalculationJobs.xml";
            }

            if (configuredPath.StartsWith("~/", StringComparison.Ordinal))
            {
                return HostingEnvironment.MapPath(configuredPath);
            }

            return Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuredPath);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
        }

        private static string NormalizeEmployee(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "ALL" : value.Trim().ToUpperInvariant();
        }

        private static void SetElementValue(XElement element, string name, string value)
        {
            XElement child = element.Element(name);

            if (child == null)
            {
                element.Add(new XElement(name, value));
            }
            else
            {
                child.Value = value ?? string.Empty;
            }
        }

        private static string ReadString(XElement element, string name)
        {
            return (string)element.Element(name) ?? string.Empty;
        }

        private static int ReadInt(XElement element, string name)
        {
            int value;
            return int.TryParse(ReadString(element, name), out value) ? value : 0;
        }

        private static DateTimeOffset ReadDate(XElement element, string name)
        {
            DateTimeOffset value;
            return DateTimeOffset.TryParse(ReadString(element, name), out value) ? value : DateTimeOffset.MinValue;
        }

        private static string FormatDate(DateTimeOffset value)
        {
            return value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
        }
    }
}
