using Art_BaBomb.Web.Data;
using Art_BaBomb.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Art_BaBomb.Web.Controllers
{
    [Authorize]
    public class ReceiptsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ReceiptsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [Authorize(Roles = "Admin,Shopper")]
        public async Task<IActionResult> Create(int? projectId)
        {
            if (projectId == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(projectId);

            if (project == null)
            {
                return NotFound();
            }

            var receipt = new Receipt
            {
                ProjectId = project.Id,
                PurchaseDate = DateTime.Today
            };

            ViewBag.ProjectName = project.Name;

            return View(receipt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Shopper")]
        public async Task<IActionResult> Create(
            [Bind("ProjectId,Vendor,PurchaseDate,TotalAmount,Notes")]
            Receipt receipt,
            IFormFile? receiptFile)
        {
            if (!IsValidReceiptFile(receiptFile, out var receiptError))
            {
                ModelState.AddModelError("receiptFile", receiptError);
            }

            var project = await _context.Projects.FindAsync(receipt.ProjectId);

            if (project == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                receipt.Vendor = receipt.Vendor.Trim();
                receipt.Notes = receipt.Notes?.Trim();
                receipt.CreatedAt = DateTime.UtcNow;

                if (receiptFile != null && receiptFile.Length > 0)
                {
                    var savedFile = await SaveUploadedFileAsync(
                        receiptFile,
                        "receipts");

                    if (savedFile.HasValue)
                    {
                        receipt.ReceiptFileName = savedFile.Value.fileName;
                        receipt.ReceiptPath = savedFile.Value.relativePath;
                        receipt.ReceiptSizeBytes = receiptFile.Length;
                    }
                }

                _context.Receipts.Add(receipt);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"Receipt from \"{receipt.Vendor}\" added successfully.";

                return RedirectToAction(
                    "Details",
                    "Projects",
                    new { id = receipt.ProjectId });
            }

            ViewBag.ProjectName = project.Name;

            return View(receipt);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var receipt = await _context.Receipts
                .Include(r => r.Project)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null)
            {
                return NotFound();
            }

            return View(receipt);
        }

        private async Task<(string fileName, string relativePath)?>
            SaveUploadedFileAsync(
                IFormFile? file,
                string folderName)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            var uploadsRoot = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                folderName);

            Directory.CreateDirectory(uploadsRoot);

            var safeFileName =
                $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            var fullPath = Path.Combine(
                uploadsRoot,
                safeFileName);

            using (var stream = new FileStream(
                fullPath,
                FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath =
                $"/uploads/{folderName}/{safeFileName}";

            return (
                Path.GetFileName(file.FileName),
                relativePath);
        }

        private static readonly string[] AllowedReceiptExtensions =
        {
            ".jpg", ".jpeg", ".png", ".webp", ".pdf"
        };

        private const long MaxReceiptFileSizeBytes =
            10 * 1024 * 1024;

        private bool IsValidReceiptFile(
            IFormFile? file,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (file == null || file.Length == 0)
            {
                return true;
            }

            var extension =
                Path.GetExtension(file.FileName);

            if (string.IsNullOrWhiteSpace(extension) ||
                !AllowedReceiptExtensions.Contains(
                    extension,
                    StringComparer.OrdinalIgnoreCase))
            {
                errorMessage =
                    "Only JPG, JPEG, PNG, WEBP, and PDF files are allowed.";

                return false;
            }

            if (file.Length > MaxReceiptFileSizeBytes)
            {
                errorMessage =
                    "Receipt files must be 10 MB or smaller.";

                return false;
            }

            return true;
        }
    }
}