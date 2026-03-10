using System.Text;
using CMCS_ST10026321.Helpers;
using CMCS_ST10026321.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMCS_ST10026321.Controllers
{
    [Authorize]
    public class ClaimController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public ClaimController(AppDbContext context, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
        }
        [Authorize(Roles = Roles.Employee + "," + Roles.Admin)]
        public IActionResult Create()
        {
            return View();
        }
        [Authorize(Roles = Roles.Admin + "," + Roles.HR)]
        public IActionResult Cam()
        {
            var claims = _context.Claims.OrderByDescending(c => c.Id).ToList();
            return View(claims);
        }
        [Authorize(Roles = Roles.HR + "," + Roles.Admin)]
        public IActionResult HR()
        {
            var claims = _context.Claims.OrderByDescending(c => c.Id).ToList();
            return View(claims);
        }

        // Display a single claim
        [Authorize(Roles = Roles.HR + "," + Roles.Admin)]
        public IActionResult Details(int id)
        {
            var claim = _context.Claims.FirstOrDefault(c => c.Id == id);
            if (claim == null) return NotFound();

            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Employee + "," + Roles.Admin)]
        public async Task<IActionResult> Create(ClaimDtocs claimDto, IFormFile file)
        {
            if (!ModelState.IsValid)
            {
                return View(claimDto);
            }

            // File validation
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Please upload a supporting document");
                return View(claimDto);
            }

            // Validate file size (5MB max)
            if (file.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("file", "File size must be less than 5MB");
                return View(claimDto);
            }

            // Validate file type
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("file", "Please upload a valid document type (PDF, DOC, DOCX, JPEG, PNG)");
                return View(claimDto);
            }

            try
            {
                // Save file
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create claim
                var claim = new Claim()
                {
                    Name = claimDto.Name,
                    Surname = claimDto.Surname,
                    Email = claimDto.Email,
                    HoursWorked = claimDto.HoursWorked,
                    HourlyRate = claimDto.HourlyRate,
                    Notes = claimDto.Notes,
                    SupportingDocumentPath = $"/uploads/{uniqueFileName}",
                    Status = "Pending Approval",
                    TotalAmount = claimDto.HoursWorked * claimDto.HourlyRate,
                    SubmittedDate = DateTime.Now
                };

                // Auto-approval logic
                if (claim.IsAutoApproved)
                {
                    claim.Status = "Auto-Approved";
                    claim.ProcessedDate = DateTime.Now;
                    claim.ProcessedBy = "System";
                }

                _context.Claims.Add(claim);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = claim.IsAutoApproved
                    ? $"Claim submitted and auto-approved! Total: R {claim.TotalAmount}"
                    : "Claim submitted successfully! Waiting for approval.";

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while submitting the claim. Please try again.");
                return View(claimDto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.HR + "," + Roles.Admin)]
        public async Task<IActionResult> Approve(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }

            claim.Status = "Approved";
            claim.ProcessedDate = DateTime.Now;
            claim.ProcessedBy = User.Identity?.Name ?? "Manual Approval";

            _context.Update(claim);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Claim #{id} has been approved.";
            return RedirectToAction("Cam");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.HR + "," + Roles.Admin)]
        public async Task<IActionResult> Reject(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }

            claim.Status = "Rejected";
            claim.ProcessedDate = DateTime.Now;
            claim.ProcessedBy = User.Identity?.Name ?? "Manual Rejection";

            _context.Update(claim);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Claim #{id} has been rejected.";
            return RedirectToAction("Cam");
        }

        // Enhanced CSV download with more details
        [Authorize(Roles = Roles.HR + "," + Roles.Admin)]
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

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        // New method for batch approval
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.HR + "," + Roles.Admin)]
        public async Task<IActionResult> BatchApprove(int[] claimIds)
        {
            if (claimIds == null || claimIds.Length == 0)
            {
                TempData["ErrorMessage"] = "No claims selected for approval.";
                return RedirectToAction("Cam");
            }

            var approvedCount = 0;
            foreach (var id in claimIds)
            {
                var claim = await _context.Claims.FindAsync(id);
                if (claim != null && claim.Status == "Pending Approval")
                {
                    claim.Status = "Approved";
                    claim.ProcessedDate = DateTime.Now;
                    claim.ProcessedBy = User.Identity?.Name ?? "Batch Approval";
                    approvedCount++;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = $"{approvedCount} claims have been approved in batch.";
            return RedirectToAction("Cam");
        }
    }
}