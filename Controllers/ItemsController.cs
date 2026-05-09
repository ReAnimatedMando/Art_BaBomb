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
            return (Path.GetFileName(file.FileName), relativePath);
        }

        // GET: Items
        public async Task<IActionResult> Index(int? projectId)
        {
            return RedirectToAction("Index", "Projects");
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
        public async Task<IActionResult> Create([Bind("ProjectId,Name,Quantity,Scene,Description,EstimatedCost")] Item item, IFormFile? imageFile)
        {

            if (!IsValidReceiptFile(imageFile, out var imageError))
            {
                ModelState.AddModelError("imageFile", imageError);
            }

            if (ModelState.IsValid)
            {
                item.Scene = item.Scene?.Trim();

                item.Status = "Needed";
                item.ActualCost = null;
                item.ImageUrl = null;

                if (imageFile != null && imageFile.Length > 0)
                {
                    var savedImage = await SaveUploadedFileAsync(imageFile, "items");
                    
                    if (savedImage.HasValue)
                    {
                        item.ImageFileName = savedImage.Value.fileName;
                        item.ImagePath = savedImage.Value.relativePath;
                        item.ImageSizeBytes = imageFile.Length;
                    }
                }

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

                return RedirectToAction("Details", "Projects", new
                {
                    id = item.ProjectId,
                    focusItemId = item.Id
                });
            }

            item.Scene = item.Scene?.Trim();

            var project = await _context.Projects.FindAsync(item.ProjectId);
            ViewBag.ProjectName = project?.Name;

            await LoadSceneOptionAsync(item.ProjectId);

            return View(item);
        }

        // GET: Items/Edit/5
        [Authorize(Roles = "Admin,Shopper")]
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
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin,Shopper")]
public async Task<IActionResult> Edit(
    int id,
    [Bind("Id,Name,Quantity,Scene,Description,EstimatedCost,ActualCost,Status")] Item item,
    IFormFile? purchaseReceiptFile,
    IFormFile? imageFile)
{
    if (id != item.Id)
    {
        return NotFound();
    }

    var existingItem = await _context.Items
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

    if (!IsValidReceiptFile(purchaseReceiptFile, out var purchaseReceiptError))
    {
        ModelState.AddModelError("purchaseReceiptFile", purchaseReceiptError);
    }

    if (!IsValidReceiptFile(imageFile, out var imageError))
    {
        ModelState.AddModelError("imageFile", imageError);
    }

    if (ModelState.IsValid)
    {
        existingItem.Name = item.Name;
        existingItem.Quantity = item.Quantity;
        existingItem.Scene = item.Scene?.Trim();
        existingItem.Description = item.Description;
        existingItem.EstimatedCost = item.EstimatedCost;
        existingItem.ActualCost = item.ActualCost;
        existingItem.Status = item.Status;

        if (imageFile != null && imageFile.Length > 0)
        {
            DeleteUploadedFile(existingItem.ImagePath);

            var savedImage = await SaveUploadedFileAsync(imageFile, "items");

            if (savedImage.HasValue)
            {
                existingItem.ImageFileName = savedImage.Value.fileName;
                existingItem.ImagePath = savedImage.Value.relativePath;
                existingItem.ImageSizeBytes = imageFile.Length;
            }
        }

        if (purchaseReceiptFile != null && purchaseReceiptFile.Length > 0)
        {
            DeleteUploadedFile(existingItem.PurchaseReceiptPath);

            var savedPurchaseFile = await SaveUploadedFileAsync(purchaseReceiptFile, "purchases");

            if (savedPurchaseFile.HasValue)
            {
                existingItem.PurchaseReceiptFileName = savedPurchaseFile.Value.fileName;
                existingItem.PurchaseReceiptPath = savedPurchaseFile.Value.relativePath;
                existingItem.PurchaseReceiptSizeBytes = purchaseReceiptFile.Length;
            }
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ItemExists(existingItem.Id))
            {
                return NotFound();
            }

            throw;
        }

        if (existingItem.NeedsPurchaseReceipt)
        {
            TempData["WarningMessage"] = $"\"{existingItem.Name}\" has an actual cost but no purchase receipt uploaded.";
        }
        else
        {
            TempData["SuccessMessage"] = $"\"{existingItem.Name}\" updated successfully.";
        }

        return RedirectToAction("Details", "Projects", new
        {
            id = existingItem.ProjectId,
            focusItemId = existingItem.Id
        });
    }

    ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", existingItem.ProjectId);

    await LoadSceneOptionAsync(existingItem.ProjectId);

    return View(existingItem);
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

            return RedirectToAction("Details", "Projects", new
            {
                id = item.ProjectId,
                focusItemId = item.Id
            });
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

            return RedirectToAction("Details", "Projects", new
            {
                id = item.ProjectId,
                focusItemId = item.Id
            });
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

            return RedirectToAction("Details", "Projects", new
            {
                id = item.ProjectId,
                focusItemId = item.Id
            });
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
                var projectId = item.ProjectId;

                var section = item.IsReturned ? "returned" : item.IsInReturnQueue ? "return" : item.IsAcquired ? "acquired" : "needed";

                var sceneKey = string.IsNullOrWhiteSpace(item.Scene) ? "Unassigned Scene" : item.Scene.Trim();

                var sceneSlug = sceneKey.Replace(" ", "-").Replace("/", "-").Replace(".", "-").ToLower();

                _context.Items.Remove(item);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"\"{item.Name}\" deleted successfully.";

                return RedirectToAction("Details", "Projects", new 
                {
                    id = projectId,
                    section = section,
                    scene = sceneSlug
                });
            }

            return RedirectToAction("Index", "Projects");
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

            return RedirectToAction("Details", "Projects", new
            {
                id = item.ProjectId,
                focusItemId = item.Id
            });
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
    [Bind("Id,ReturnNotes,ReturnLocation,ReturnByDate,IsReturned")]
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
        .Include(i => i.Project)
        .FirstOrDefaultAsync(i => i.Id == id);

    if (existingItem == null)
    {
        return NotFound();
    }

    if (!IsValidReceiptFile(returnReceiptFile, out var receiptError))
    {
        ModelState.AddModelError("returnReceiptFile", receiptError);
    }

    ModelState.Remove(nameof(Item.ProjectId));
    ModelState.Remove(nameof(Item.Name));
    ModelState.Remove(nameof(Item.Project));

    if (ModelState.IsValid)
    {
        try
        {
            existingItem.ReturnNotes = item.ReturnNotes;
            existingItem.ReturnLocation = item.ReturnLocation;
            existingItem.ReturnByDate = item.ReturnByDate;

            if (removeReturnReceipt)
            {
                DeleteUploadedFile(existingItem.ReturnReceiptPath);

                existingItem.ReturnReceiptFileName = null;
                existingItem.ReturnReceiptPath = null;
                existingItem.ReturnReceiptSizeBytes = null;
            }

            if (returnReceiptFile != null && returnReceiptFile.Length > 0)
            {
                DeleteUploadedFile(existingItem.ReturnReceiptPath);

                var savedFile = await SaveUploadedFileAsync(returnReceiptFile, "returns");

                if (savedFile.HasValue)
                {
                    existingItem.ReturnReceiptFileName = savedFile.Value.fileName;
                    existingItem.ReturnReceiptPath = savedFile.Value.relativePath;
                    existingItem.ReturnReceiptSizeBytes = returnReceiptFile.Length;
                }
            }

            if (removeFromReturnWorkflow)
            {
                existingItem.IsReturnRequired = false;
                existingItem.IsReturned = false;
                existingItem.ReturnedAt = null;
                existingItem.ReturnLocation = null;
                existingItem.ReturnByDate = null;
                existingItem.ReturnNotes = null;
            }
            else
            {
                existingItem.IsReturnRequired = true;
                existingItem.IsReturned = item.IsReturned;

                if (existingItem.IsReturned)
                {
                    existingItem.ReturnedAt ??= DateTime.UtcNow;
                }
                else
                {
                    existingItem.ReturnedAt = null;
                }
            }

            await _context.SaveChangesAsync();

            if (existingItem.MissingReturnByDate)
            {
                TempData["WarningMessage"] = $"\"{existingItem.Name}\" is in the return workflow but has no return-by date.";
            }
            else if (existingItem.HasPastReturnByDate)
            {
                TempData["WarningMessage"] = $"\"{existingItem.Name}\" has a return-by date in the past.";
            }
            else if (existingItem.NeedsReturnReceipt)
            {
                TempData["WarningMessage"] = $"\"{existingItem.Name}\" was marked returned but is missing a return receipt.";
            }
            else
            {
                TempData["SuccessMessage"] = $"\"{existingItem.Name}\" return info updated successfully.";
            }

            return RedirectToAction("Details", "Projects", new
            {
                id = existingItem.ProjectId,
                focusItemId = existingItem.Id
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ItemExists(existingItem.Id))
            {
                return NotFound();
            }

            throw;
        }
    }

    return View(existingItem);
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

            return RedirectToAction("Details", "Projects", new
            {
                id = item.ProjectId,
                focusItemId = item.Id
            });
        }
    }
}
