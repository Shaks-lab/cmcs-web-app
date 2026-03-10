using CMCS_ST10026321.Models;
using Microsoft.EntityFrameworkCore;

namespace CMCS_ST10026321.Models
{
    public interface IClaimAutomationService
    {
        Task ProcessPendingClaimsAsync();
        Task<int> AutoApproveClaimsAsync();
        Task GenerateMonthlyReportAsync();
        Task<string> GenerateInvoiceAsync(int claimId);
    }

    public class ClaimAutomationService : IClaimAutomationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ClaimAutomationService> _logger;
        private readonly IWebHostEnvironment _environment;

        public ClaimAutomationService(AppDbContext context, ILogger<ClaimAutomationService> logger, IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        public async Task ProcessPendingClaimsAsync()
        {
            try
            {
                _logger.LogInformation("Starting ProcessPendingClaimsAsync...");

                var pendingClaims = await _context.Claims
                    .Where(c => c.Status == "Pending Approval")
                    .ToListAsync();

                var autoApprovedCount = 0;

                foreach (var claim in pendingClaims)
                {
                    if (IsEligibleForAutoApproval(claim))
                    {
                        claim.Status = "Auto-Approved";
                        claim.ProcessedDate = DateTime.Now;
                        claim.ProcessedBy = "Automation System";
                        autoApprovedCount++;

                        _logger.LogInformation($"Claim #{claim.Id} auto-approved. Amount: R{claim.TotalAmount}");
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"ProcessPendingClaimsAsync completed. Auto-approved {autoApprovedCount} claims.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing pending claims");
                throw;
            }
        }

        public async Task<int> AutoApproveClaimsAsync()
        {
            try
            {
                _logger.LogInformation("Starting AutoApproveClaimsAsync...");

                var pendingClaims = await _context.Claims
                    .Where(c => c.Status == "Pending Approval")
                    .ToListAsync();

                _logger.LogInformation($"Found {pendingClaims.Count} pending claims");

                var autoApprovableClaims = pendingClaims
                    .Where(c => IsEligibleForAutoApproval(c))
                    .ToList();

                _logger.LogInformation($"Found {autoApprovableClaims.Count} claims eligible for auto-approval");

                foreach (var claim in autoApprovableClaims)
                {
                    claim.Status = "Auto-Approved";
                    claim.ProcessedDate = DateTime.Now;
                    claim.ProcessedBy = "Automation System";

                    _logger.LogInformation($"Auto-approving claim #{claim.Id} - Amount: R{claim.TotalAmount}, Hours: {claim.HoursWorked}, Rate: R{claim.HourlyRate}");
                }

                var count = autoApprovableClaims.Count;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully auto-approved {count} claims.");
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoApproveClaimsAsync");
                throw;
            }
        }

        private bool IsEligibleForAutoApproval(Claim claim)
        {
            try
            {
                if (claim == null) return false;

                return claim.TotalAmount <= 5000 &&
                       claim.HoursWorked <= 160 &&
                       claim.HoursWorked >= 1 &&
                       claim.HourlyRate >= 50 &&
                       claim.HourlyRate <= 500;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking eligibility for claim {claim?.Id}");
                return false;
            }
        }

        public async Task GenerateMonthlyReportAsync()
        {
            try
            {
                var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var monthlyClaims = await _context.Claims
                    .Where(c => c.SubmittedDate >= startDate && c.SubmittedDate <= endDate)
                    .ToListAsync();

                var approvedClaims = monthlyClaims.Where(c => c.Status == "Approved" || c.Status == "Auto-Approved").ToList();
                var totalAmount = approvedClaims.Any() ? approvedClaims.Sum(c => c.TotalAmount) : 0;
                var averageAmount = approvedClaims.Any() ? approvedClaims.Average(c => c.TotalAmount) : 0;

                var reportData = new
                {
                    Period = $"{startDate:MMMM yyyy}",
                    TotalClaims = monthlyClaims.Count,
                    ApprovedClaims = approvedClaims.Count,
                    PendingClaims = monthlyClaims.Count(c => c.Status == "Pending Approval"),
                    RejectedClaims = monthlyClaims.Count(c => c.Status == "Rejected"),
                    TotalAmount = totalAmount,
                    AverageClaimAmount = averageAmount
                };

                await GenerateReportFileAsync(reportData, startDate);
                _logger.LogInformation($"Monthly report generated for {reportData.Period}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly report");
                throw;
            }
        }

        private async Task GenerateReportFileAsync(dynamic reportData, DateTime reportDate)
        {
            try
            {
                var reportsFolder = Path.Combine(_environment.WebRootPath, "reports");
                if (!Directory.Exists(reportsFolder))
                {
                    Directory.CreateDirectory(reportsFolder);
                }

                var reportContent = $@"
============================================
   MONTHLY CLAIMS REPORT - {reportData.Period}
============================================

Report Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

SUMMARY STATISTICS
--------------------------------------------
Total Claims Submitted: {reportData.TotalClaims}
Approved Claims: {reportData.ApprovedClaims}
Pending Claims: {reportData.PendingClaims}
Rejected Claims: {reportData.RejectedClaims}

FINANCIAL SUMMARY
--------------------------------------------
Total Amount Approved: R {reportData.TotalAmount:N2}
Average Claim Amount: R {reportData.AverageClaimAmount:N2}

============================================
";

                var fileName = $"MonthlyReport_{reportDate:yyyyMM}.txt";
                var filePath = Path.Combine(reportsFolder, fileName);

                await System.IO.File.WriteAllTextAsync(filePath, reportContent);
                _logger.LogInformation($"Report saved to: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report file");
                throw;
            }
        }

        public async Task<string> GenerateInvoiceAsync(int claimId)
        {
            try
            {
                var claim = await _context.Claims.FindAsync(claimId);
                if (claim == null)
                    throw new ArgumentException($"Claim with ID {claimId} not found");

                if (claim.Status != "Approved" && claim.Status != "Auto-Approved")
                    throw new InvalidOperationException("Invoice can only be generated for approved claims");

                var invoiceContent = $@"
============================================
                    INVOICE
============================================

Invoice Number: INV-{claimId:D6}
Date: {DateTime.Now:yyyy-MM-dd}

LECTURER DETAILS
--------------------------------------------
Name: {claim.Name} {claim.Surname}
Email: {claim.Email}

CLAIM DETAILS
--------------------------------------------
Hours Worked: {claim.HoursWorked}
Hourly Rate: R {claim.HourlyRate:N2}
Total Amount: R {claim.TotalAmount:N2}

Status: {claim.Status}
Processed By: {claim.ProcessedBy}
Processed Date: {claim.ProcessedDate:yyyy-MM-dd}

============================================
        Thank you for your service
============================================
";

                var invoicesFolder = Path.Combine(_environment.WebRootPath, "invoices");
                if (!Directory.Exists(invoicesFolder))
                {
                    Directory.CreateDirectory(invoicesFolder);
                }

                var fileName = $"Invoice_{claimId}_{DateTime.Now:yyyyMMddHHmmss}.txt";
                var filePath = Path.Combine(invoicesFolder, fileName);

                await System.IO.File.WriteAllTextAsync(filePath, invoiceContent);
                _logger.LogInformation($"Invoice generated: {filePath}");

                return $"/invoices/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating invoice for claim {claimId}");
                throw;
            }
        }
    }
}