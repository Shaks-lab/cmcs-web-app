using System.Text;
using CMCS_ST10026321.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMCS_ST10026321.Controllers
{
    public class HRController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IClaimAutomationService _automationService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<HRController> _logger; // Changed to ILogger<HRController>

        public HRController(
            AppDbContext context,
            IClaimAutomationService automationService,
            IWebHostEnvironment environment,
            ILogger<HRController> logger) // Changed to ILogger<HRController>
        {
            _context = context;
            _automationService = automationService;
            _environment = environment;
            _logger = logger;
        }

        public IActionResult HR()
        {
            var claims = _context.Claims
                .OrderByDescending(c => c.Id)
                .ToList();
            return View(claims);
        }

        public IActionResult ClaimSummary()
        {
            var claims = _context.Claims.ToList();
            return View(claims);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutomateClaims()
        {
            try
            {
                _logger.LogInformation("Starting automation process...");

                var autoApprovedCount = await _automationService.AutoApproveClaimsAsync();

                _logger.LogInformation($"Automation completed. {autoApprovedCount} claims auto-approved.");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Automation completed. {autoApprovedCount} claims auto-approved.",
                        count = autoApprovedCount
                    });
                }

                TempData["SuccessMessage"] = autoApprovedCount > 0
                    ? $"Automation completed. {autoApprovedCount} claims auto-approved."
                    : "Automation completed. No claims met auto-approval criteria.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during automation");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Error: {ex.Message}"
                    });
                }
                TempData["ErrorMessage"] = $"Error during automation: {ex.Message}";
            }

            return RedirectToAction("HR");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateMonthlyReport()
        {
            try
            {
                _logger.LogInformation("Starting report generation...");

                await _automationService.GenerateMonthlyReportAsync();

                var reportsFolder = Path.Combine(_environment.WebRootPath, "reports");

                if (!Directory.Exists(reportsFolder))
                {
                    Directory.CreateDirectory(reportsFolder);
                }

                var latestReport = Directory.GetFiles(reportsFolder, "MonthlyReport_*.txt")
                                             .OrderByDescending(f => f)
                                             .FirstOrDefault();

                if (latestReport != null)
                {
                    var fileName = Path.GetFileName(latestReport);
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(latestReport);

                    return File(fileBytes, "text/plain", fileName);
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Report generated successfully." });
                }

                TempData["SuccessMessage"] = "Monthly report generated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Error: {ex.Message}"
                    });
                }
                TempData["ErrorMessage"] = $"Error generating report: {ex.Message}";
            }

            return RedirectToAction("HR");
        }

        public async Task<IActionResult> GenerateInvoice(int id)
        {
            try
            {
                _logger.LogInformation($"Generating invoice for claim #{id}");

                var claim = await _context.Claims.FindAsync(id);
                if (claim == null)
                {
                    _logger.LogWarning($"Claim #{id} not found");
                    TempData["ErrorMessage"] = $"Claim #{id} not found.";
                    return RedirectToAction("HR");
                }

                if (claim.Status != "Approved" && claim.Status != "Auto-Approved")
                {
                    _logger.LogWarning($"Claim #{id} is not approved (Status: {claim.Status})");
                    TempData["ErrorMessage"] = $"Cannot generate invoice for claim #{id} - claim status is '{claim.Status}'. Only approved claims can generate invoices.";
                    return RedirectToAction("HR");
                }

                var invoicesFolder = Path.Combine(_environment.WebRootPath, "invoices");
                if (!Directory.Exists(invoicesFolder))
                {
                    Directory.CreateDirectory(invoicesFolder);
                }

                var invoiceContent = $@"
=================================================================
                         CMCS INVOICE
=================================================================

Invoice Number: INV-{claim.Id:D6}
Date Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Claim ID: {claim.Id}

LECTURER DETAILS
-----------------------------------------------------------------
Name: {claim.Name} {claim.Surname}
Email: {claim.Email}

CLAIM DETAILS
-----------------------------------------------------------------
Hours Worked: {claim.HoursWorked}
Hourly Rate: R {claim.HourlyRate:F2}
Total Amount: R {claim.TotalAmount:F2}

Submission Date: {claim.SubmittedDate:yyyy-MM-dd}
Status: {claim.Status}
Processed By: {claim.ProcessedBy ?? "N/A"}
Processed Date: {claim.ProcessedDate:yyyy-MM-dd}

=================================================================
              This is a system-generated invoice
=================================================================";

                var fileName = $"Invoice_{claim.Id}_{claim.Name}_{claim.Surname}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var filePath = Path.Combine(invoicesFolder, fileName);

                await System.IO.File.WriteAllTextAsync(filePath, invoiceContent);
                _logger.LogInformation($"Invoice saved to: {filePath}");

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

                return File(fileBytes, "text/plain", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating invoice for claim {id}");
                TempData["ErrorMessage"] = $"Error generating invoice: {ex.Message}";
                return RedirectToAction("HR");
            }
        }

        public async Task<IActionResult> ViewInvoice(int id)
        {
            try
            {
                _logger.LogInformation($"Viewing invoice for claim #{id}");

                var claim = await _context.Claims.FindAsync(id);
                if (claim == null)
                {
                    _logger.LogWarning($"Claim #{id} not found");
                    TempData["ErrorMessage"] = $"Claim #{id} not found.";
                    return RedirectToAction("HR");
                }

                var invoicesFolder = Path.Combine(_environment.WebRootPath, "invoices");
                if (!Directory.Exists(invoicesFolder))
                {
                    TempData["ErrorMessage"] = $"No invoice found for claim #{id}.";
                    return RedirectToAction("HR");
                }

                var matchingInvoiceFiles = Directory.GetFiles(invoicesFolder, $"Invoice_{id}_*.txt")
                                             .OrderByDescending(f => f)
                                             .ToList();

                if (matchingInvoiceFiles.Any())
                {
                    var latestInvoiceFile = matchingInvoiceFiles.First();
                    var invoiceFileName = Path.GetFileName(latestInvoiceFile);
                    var invoiceFileBytes = await System.IO.File.ReadAllBytesAsync(latestInvoiceFile);

                    return File(invoiceFileBytes, "text/plain", invoiceFileName);
                }

                _logger.LogInformation($"No existing invoice found for claim #{id}. Generating now...");

                var invoiceContent = $@"
=================================================================
                         CMCS INVOICE
=================================================================

Invoice Number: INV-{claim.Id:D6}
Date Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Claim ID: {claim.Id}

LECTURER DETAILS
-----------------------------------------------------------------
Name: {claim.Name} {claim.Surname}
Email: {claim.Email}

CLAIM DETAILS
-----------------------------------------------------------------
Hours Worked: {claim.HoursWorked}
Hourly Rate: R {claim.HourlyRate:F2}
Total Amount: R {claim.TotalAmount:F2}

Submission Date: {claim.SubmittedDate:yyyy-MM-dd}
Status: {claim.Status}
Processed By: {claim.ProcessedBy ?? "N/A"}
Processed Date: {claim.ProcessedDate:yyyy-MM-dd}

=================================================================
              This is a system-generated invoice
=================================================================";

                var newInvoiceFileName = $"Invoice_{claim.Id}_{claim.Name}_{claim.Surname}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var newInvoiceFilePath = Path.Combine(invoicesFolder, newInvoiceFileName);

                await System.IO.File.WriteAllTextAsync(newInvoiceFilePath, invoiceContent);

                var newInvoiceFileBytes = await System.IO.File.ReadAllBytesAsync(newInvoiceFilePath);
                return File(newInvoiceFileBytes, "text/plain", newInvoiceFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error viewing invoice for claim {id}");
                TempData["ErrorMessage"] = $"Error viewing invoice: {ex.Message}";
                return RedirectToAction("HR");
            }
        }

        public IActionResult DownloadSummary()
        {
            var claims = _context.Claims.ToList();

            var builder = new StringBuilder();
            builder.AppendLine("Id,Name,Surname,Email,HoursWorked,HourlyRate,Notes,Status,TotalAmount,SubmittedDate,ProcessedDate,ProcessedBy");

            foreach (var claim in claims)
            {
                builder.AppendLine($"{claim.Id},{EscapeCsvField(claim.Name)},{EscapeCsvField(claim.Surname)},{EscapeCsvField(claim.Email)},{claim.HoursWorked},{claim.HourlyRate},{EscapeCsvField(claim.Notes)},{claim.Status},{claim.TotalAmount},{claim.SubmittedDate:yyyy-MM-dd},{claim.ProcessedDate:yyyy-MM-dd},{EscapeCsvField(claim.ProcessedBy)}");
            }

            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"ClaimSummary_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        public async Task<IActionResult> LecturerManagement()
        {
            var lecturers = await _context.Claims
                .GroupBy(c => new { c.Name, c.Surname, c.Email })
                .Select(g => new
                {
                    Name = g.Key.Name,
                    Surname = g.Key.Surname,
                    Email = g.Key.Email,
                    TotalClaims = g.Count(),
                    TotalAmount = g.Sum(c => c.TotalAmount),
                    LastSubmission = g.Max(c => c.SubmittedDate)
                })
                .ToListAsync();

            return View(lecturers);
        }

        [HttpGet]
        public async Task<IActionResult> TestAutomation()
        {
            try
            {
                var claims = await _context.Claims.ToListAsync();
                var pendingCount = claims.Count(c => c.Status == "Pending Approval");
                var approvedCount = claims.Count(c => c.Status == "Approved" || c.Status == "Auto-Approved");
                var rejectedCount = claims.Count(c => c.Status == "Rejected");

                return Json(new
                {
                    success = true,
                    totalClaims = claims.Count,
                    pendingClaims = pendingCount,
                    approvedClaims = approvedCount,
                    rejectedClaims = rejectedCount,
                    message = "Database connection working"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestAutomation");
                return Json(new { success = false, error = ex.Message });
            }
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
    }
}