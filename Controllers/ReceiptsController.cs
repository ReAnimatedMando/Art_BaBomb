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

        public async Task<IActionResult> Index(int? projectId)
        {
            if (projectId == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.Receipts)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                return NotFound();
            }

            var receipts = project.Receipts
                .OrderByDescending(r => r.PurchaseDate)
                .ThenByDescending(r => r.CreatedAt)
                .ToList();

            ViewBag.Project = project;
            ViewBag.TotalSpent = receipts.Sum(r => r.TotalAmount);
            ViewBag.RemainingBudget = project.Budget - receipts.Sum(r => r.TotalAmount);

            return View(receipts);
        }

        private void DeleteUploadedFile(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            var trimmedPath = relativePath
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);

            var fullPath = Path.Combine(
                _environment.WebRootPath,
                trimmedPath);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
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

        [Authorize(Roles = "Admin,Shopper")]
        public async Task<IActionResult> Edit(int? id)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Shopper")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,ProjectId,Vendor,PurchaseDate,TotalAmount,Notes")]
            Receipt receipt,
            IFormFile? receiptFile)
        {
            if (id != receipt.Id)
            {
                return NotFound();
            }

            var existingReceipt = await _context.Receipts
                .Include(r => r.Project)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (existingReceipt == null)
            {
                return NotFound();
            }

            if (!IsValidReceiptFile(receiptFile, out var receiptError))
            {
                ModelState.AddModelError("receiptFile", receiptError);
            }

            if (ModelState.IsValid)
            {
                existingReceipt.Vendor = receipt.Vendor.Trim();
                existingReceipt.PurchaseDate = receipt.PurchaseDate;
                existingReceipt.TotalAmount = receipt.TotalAmount;
                existingReceipt.Notes = receipt.Notes?.Trim();

                if (receiptFile != null && receiptFile.Length > 0)
                {
                    DeleteUploadedFile(existingReceipt.ReceiptPath);

                    var savedFile = await SaveUploadedFileAsync(
                        receiptFile,
                        "receipts");

                    if (savedFile.HasValue)
                    {
                        existingReceipt.ReceiptFileName =
                            savedFile.Value.fileName;

                        existingReceipt.ReceiptPath =
                            savedFile.Value.relativePath;

                        existingReceipt.ReceiptSizeBytes =
                            receiptFile.Length;
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"Receipt from \"{existingReceipt.Vendor}\" updated successfully.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = existingReceipt.Id });
            }

            receipt.Project = existingReceipt.Project;

            return View(receipt);
        }

        [Authorize(Roles = "Admin,Shopper")]
        public async Task<IActionResult> Delete(int? id)
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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Shopper")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var receipt = await _context.Receipts.FindAsync(id);

            if (receipt == null)
            {
                return NotFound();
            }

            var projectId = receipt.ProjectId;

            DeleteUploadedFile(receipt.ReceiptPath);

            _context.Receipts.Remove(receipt);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Receipt from \"{receipt.Vendor}\" deleted successfully.";

            return RedirectToAction(
                nameof(Index),
                new { projectId });
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