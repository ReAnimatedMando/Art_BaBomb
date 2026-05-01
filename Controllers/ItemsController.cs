using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Art_BaBomb.Web.Data;
using Art_BaBomb.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Art_BaBomb.Web.Controllers
{
    [Authorize]
    public class ItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ItemsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
        
        // Save files
        private async Task<(string fileName, string relativePath)?> SaveUploadedFileAsync(IFormFile? file, string folderName)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", folderName);
            Directory.CreateDirectory(uploadsRoot);

            var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullPath = Path.Combine(uploadsRoot, safeFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/{folderName}/{safeFileName}";
            return (file.FileName, relativePath);
        }

        // GET: Items
        public async Task<IActionResult> Index(int? projectId)
        {
            var query = _context.Items.Include(i => i.Project).AsQueryable();

            if (projectId.HasValue)
            {
                query = query.Where(i => i.ProjectId == projectId.Value);
                ViewBag.ProjectId = projectId.Value;
                ViewBag.ProjectName = await _context.Projects
                    .Where(p => p.Id == projectId.Value)
                    .Select(p => p.Name)
                    .FirstOrDefaultAsync();
            }

            return View(await query.ToListAsync());
        }

        // GET: Items/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(i => i.Project)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // GET: Items/Create
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

            var item = new Item
            {
                ProjectId = projectId.Value,
                Status = "Needed"
            };

            ViewBag.ProjectName = project.Name;

            await LoadSceneOptionAsync(projectId.Value);

            return View(item);
        }

        // POST: Items/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Shopper")]
        public async Task<IActionResult> Create([Bind("ProjectId,Name,Quantity,Scene,Description,EstimatedCost,ActualCost,Status,ImageUrl")] Item item)
        {
            if (ModelState.IsValid)
            {
                item.Scene = item.Scene?.Trim();

                _context.Items.Add(item);
                await _context.SaveChangesAsync();

                if (item.NeedsPurchaseReceipt)
                {
                    TempData["WarningMessage"] = $"\"{item.Name}\" has an actual cost but no purchase receipt uploaded.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"\"{item.Name}\" created successfully.";
                }

                return RedirectToAction("Details", "Projects", new { id = item.ProjectId });
            }

            item.Scene = item.Scene?.Trim();

            var project = await _context.Projects.FindAsync(item.ProjectId);
            ViewBag.ProjectName = project?.Name;

            await LoadSceneOptionAsync(item.ProjectId);

            return View(item);
        }

        // GET: Items/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            if (item.IsReturnRequired || item.IsReturned)
            {
                TempData["ErrorMessage"] = "Items in the return workflow must be updated from return info.";
                return RedirectToAction(nameof(ReturnInfo), new { id = item.Id });
            }

            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", item.ProjectId);

            await LoadSceneOptionAsync(item.ProjectId);

            return View(item);
        }

        // POST: Items/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,ProjectId,Name,Quantity,Scene,Description,EstimatedCost,ActualCost,Status,ImageUrl,PurchaseReceiptFileName,PurchaseReceiptPath")] Item item,
            IFormFile? purchaseReceiptFile)
        {
            if (id != item.Id)
            {
                return NotFound();
            }

            var existingItem = await _context.Items
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);

            if (existingItem == null)
            {
                return NotFound();
            }

            if (existingItem.IsReturnRequired || existingItem.IsReturned)
            {
                TempData["ErrorMessage"] = "Items in the return workflow must be updated from return info.";
                return RedirectToAction(nameof(ReturnInfo), new { id = existingItem.Id });
            }

            // Preserve existing receipt values unless a new file is uploaded
            item.PurchaseReceiptFileName = existingItem.PurchaseReceiptFileName;
            item.PurchaseReceiptPath = existingItem.PurchaseReceiptPath;
            item.PurchaseReceiptSizeBytes = existingItem.PurchaseReceiptSizeBytes;

            if (!IsValidReceiptFile(purchaseReceiptFile, out var purchaseReceiptError))
            {
                ModelState.AddModelError("purchaseReceiptFile", purchaseReceiptError);
            }

            if (ModelState.IsValid)
            {
                // Normalize scene text before saving
                item.Scene = item.Scene?.Trim();

                if (purchaseReceiptFile != null && purchaseReceiptFile.Length > 0)
                {
                    DeleteUploadedFile(existingItem.PurchaseReceiptPath);

                    var savedPurchaseFile = await SaveUploadedFileAsync(purchaseReceiptFile, "purchases");
                    if (savedPurchaseFile.HasValue)
                    {
                        item.PurchaseReceiptFileName = savedPurchaseFile.Value.fileName;
                        item.PurchaseReceiptPath = savedPurchaseFile.Value.relativePath;
                        item.PurchaseReceiptSizeBytes = purchaseReceiptFile.Length;
                    }
                }

                try
                {
                    _context.Update(item);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemExists(item.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                if (item.NeedsPurchaseReceipt)
                {
                    TempData["WarningMessage"] = $"\"{item.Name}\" has an actual cost but no purchase receipt uploaded.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"\"{item.Name}\" updated successfully.";
                }

                return RedirectToAction("Details", "Projects", new { id = item.ProjectId });
            }

            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", item.ProjectId);

            await LoadSceneOptionAsync(item.ProjectId);

            return View(item);
        }

        // Get: load existing scenes for selected project
        private async Task LoadSceneOptionAsync(int projectId)
        {
            ViewBag.ExistingScenes = await _context.Items
                .Where(i => i.ProjectId == projectId && !string.IsNullOrWhiteSpace(i.Scene))
                .Select(i => i.Scene!.Trim())
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();
        }

        // POST: Adjust Quantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Shopper")]
        public async Task<IActionResult> AdjustQuantity(int id, int delta)
        {
            var item = await _context.Items.FindAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            var newQuantity = item.Quantity + delta;
            item.Quantity = Math.Max(1, newQuantity);

            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, itemId = item.Id, quantity = item.Quantity });
            }

            return RedirectToAction("Details", "Projects", new { id = item.ProjectId });
        }

        // POST: Mark as Acquired / Mark as Needed
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Shopper")]
        public async Task<IActionResult> MarkAcquired(int id)
        {
            var item = await _context.Items.FindAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            item.Status = "Acquired";
            item.IsReturned = false;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Marked \"{item.Name}\" as acquired.";
            return RedirectToAction("Details", "Projects", new { id = item.ProjectId });
        }

        // POST: Mark as Needed
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Shopper")]
        public async Task<IActionResult> MarkNeeded(int id)
        {
            var item = await _context.Items.FindAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            item.Status = "Needed";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Moved \"{item.Name}\" back to needed.";
            return RedirectToAction("Details", "Projects", new { id = item.ProjectId });
        }

        // GET: Items/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(i => i.Project)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // POST: Items/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item != null)
            {
                _context.Items.Remove(item);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.Id == id);
        }

        // POST: Update Description
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Shopper")]
        public async Task<IActionResult> UpdateDescription(int id, string? description)
        {
            var item = await _context.Items.FindAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            item.Description = string.IsNullOrWhiteSpace(description)
                ? null
                : description.Trim();

            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    itemId = item.Id,
                    description = item.Description ?? ""
                });
            }

            return RedirectToAction("Details", "Projects", new { id = item.ProjectId });
        }

        // GET: Items/PurchaseReceipt/5
        [Authorize(Roles = "Admin,Shopper")]
        public async Task<IActionResult> PurchaseReceipt(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(i => i.Project)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // POST: Purchase Receipt
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Shopper")]
        public async Task<IActionResult> PurchaseReceipt(int id, IFormFile? purchaseReceiptFile)
        {
            var item = await _context.Items
                .Include(i => i.Project)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                return NotFound();
            }

            if (!IsValidReceiptFile(purchaseReceiptFile, out var purchaseReceiptError))
            {
                ModelState.AddModelError("purchaseReceiptFile", purchaseReceiptError);
            }

            if (!ModelState.IsValid)
            {
                return View(item);
            }

            if (purchaseReceiptFile != null && purchaseReceiptFile.Length > 0)
            {
                DeleteUploadedFile(item.PurchaseReceiptPath);

                var uploadResult = await SaveUploadedFileAsync(purchaseReceiptFile, "purchases");

                if (uploadResult.HasValue)
                {
                    item.PurchaseReceiptFileName = uploadResult.Value.fileName;
                    item.PurchaseReceiptPath = uploadResult.Value.relativePath;
                    item.PurchaseReceiptSizeBytes = purchaseReceiptFile.Length;
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Purchase receipt updated successfully.";
            return RedirectToAction(nameof(Details), new { id = item.Id });
        }

        private static readonly string[] AllowedReceiptExtensions = 
        {
            ".jpg", ".jpeg", ".png", ".webp", ".pdf"
        };

        private const long MaxReceiptFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        private bool IsValidReceiptFile(IFormFile? file, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (file == null || file.Length == 0)
            {
                return true;
            }

            var extension = Path.GetExtension(file.FileName);

            if (string.IsNullOrWhiteSpace(extension) ||
                !AllowedReceiptExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                errorMessage = "Only JPG, JPEG, PNG, WEBP, and PDF files are allowed.";
                return false;
            }

            if (file.Length > MaxReceiptFileSizeBytes)
            {
                errorMessage = "Receipt files must be 10 MB or smaller.";
                return false;
            }

            return true;
        }

        // POST: Delete Purchase Receipt
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Shopper")]
        public async Task<IActionResult> DeletePurchaseReceipt(int id)
        {
            var item = await _context.Items.FindAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            DeleteUploadedFile(item.PurchaseReceiptPath);

            item.PurchaseReceiptFileName = null;
            item.PurchaseReceiptPath = null;
            item.PurchaseReceiptSizeBytes = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Purchase receipt deleted successfully.";
            return RedirectToAction(nameof(Details), new { id = item.Id });
        }

        // DELETE: Delete Uploaded File
        private void DeleteUploadedFile(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            var trimmedPath = relativePath.TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);

            var fullPath = Path.Combine(_environment.WebRootPath, trimmedPath);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        // GET: ReturnInfo
        [Authorize(Roles = "Admin,Shopper")]
        public async Task<IActionResult> ReturnInfo(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(i => i.Project)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // POST: ReturnInfo
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Shopper")]
        public async Task<IActionResult> ReturnInfo(
            int id,
            [Bind("Id,ProjectId,Name,Scene,Description,EstimatedCost,ActualCost,Status,ImageUrl,IsReturnRequired,ReturnNotes,ReturnLocation,ReturnByDate,IsReturned,ReturnedAt,PurchaseReceiptFileName,PurchaseReceiptPath,ReturnReceiptFileName,ReturnReceiptPath")]
            Item item,
            IFormFile? returnReceiptFile,
            bool removeReturnReceipt = false,
            bool removeFromReturnWorkflow = false)
        {
            if (id != item.Id)
            {
                return NotFound();
            }

            var existingItem = await _context.Items
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);

            if (existingItem == null)
            {
                return NotFound();
            }

            item.PurchaseReceiptFileName = existingItem.PurchaseReceiptFileName;
            item.PurchaseReceiptPath = existingItem.PurchaseReceiptPath;
            item.PurchaseReceiptSizeBytes = existingItem.PurchaseReceiptSizeBytes;
            item.ReturnReceiptFileName = existingItem.ReturnReceiptFileName;
            item.ReturnReceiptPath = existingItem.ReturnReceiptPath;
            item.ReturnReceiptSizeBytes = existingItem.ReturnReceiptSizeBytes;

            if (!IsValidReceiptFile(returnReceiptFile, out var receiptError))
            {
                ModelState.AddModelError("returnReceiptFile", receiptError);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (removeReturnReceipt)
                    {
                        DeleteUploadedFile(existingItem.ReturnReceiptPath);

                        item.ReturnReceiptFileName = null;
                        item.ReturnReceiptPath = null;
                        item.ReturnReceiptSizeBytes = null;
                    }

                    if (returnReceiptFile != null && returnReceiptFile.Length > 0)
                    {
                        DeleteUploadedFile(existingItem.ReturnReceiptPath);

                        var savedFile = await SaveUploadedFileAsync(returnReceiptFile, "returns");
                        if (savedFile.HasValue)
                        {
                            item.ReturnReceiptFileName = savedFile.Value.fileName;
                            item.ReturnReceiptPath = savedFile.Value.relativePath;
                            item.ReturnReceiptSizeBytes = returnReceiptFile.Length;
                        }
                    }

                    if (removeFromReturnWorkflow)
                    {
                        item.IsReturnRequired = false;
                        item.IsReturned = false;
                        item.ReturnedAt = null;
                        item.ReturnLocation = null;
                        item.ReturnByDate = null;
                        item.ReturnNotes = null;
                    }
                    else
                    {
                        item.IsReturnRequired = true;

                        if (item.IsReturned)
                        {
                            item.ReturnedAt ??= DateTime.UtcNow;
                        }   
                        else
                        {
                            item.ReturnedAt = null;
                        }
                    }

                    _context.Update(item);
                    await _context.SaveChangesAsync();

                    if (item.MissingReturnByDate)
                    {
                        TempData["WarningMessage"] = $"\"{item.Name}\" is in the return workflow but has no return-by date.";
                    }
                    else if (item.HasPastReturnByDate)
                    {
                        TempData["WarningMessage"] = $"\"{item.Name}\" has a return-by date in the past.";
                    }
                    else if (item.NeedsReturnReceipt)
                    {
                        TempData["WarningMessage"] = $"\"{item.Name}\" was marked returned but is missing a return receipt.";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = $"\"{item.Name}\" return info updated successfully.";
                    }

                    return RedirectToAction(nameof(Details), new { id = item.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Items.Any(e => e.Id == item.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }
            }

            item.Project = await _context.Projects.FindAsync(item.ProjectId);
            return View(item);
        }

        // POST: Mark as Returned
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Shopper")]
        public async Task<IActionResult> MarkReturned(int id)
        {
            var item = await _context.Items.FindAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            item.IsReturnRequired = true;
            item.IsReturned = true;
            item.ReturnedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (item.NeedsReturnReceipt)
            {
                TempData["WarningMessage"] = $"\"{item.Name}\" is marked as returned but is missing a return receipt.";
            }
            else
            {
                TempData["SuccessMessage"] = $"\"{item.Name}\" marked as returned.";
            }

            return RedirectToAction(nameof(Details), new { id = item.Id });
        }
    }
}
