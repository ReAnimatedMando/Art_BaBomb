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

            return View(item);
        }

        // POST: Items/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProjectId,Name,ItemNumber,Category,Description,EstimatedCost,ActualCost,Status,ImageUrl")] Item item)
        {
            if (ModelState.IsValid)
            {
                _context.Items.Add(item);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Projects", new { id = item.ProjectId });
            }

            var project = await _context.Projects.FindAsync(item.ProjectId);
            ViewBag.ProjectName = project?.Name;

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
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", item.ProjectId);
            return View(item);
        }

        // POST: Items/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProjectId,Name,ItemNumber,Category,Description,EstimatedCost,ActualCost,Status,ImageUrl,IsReturnRequired,ReturnNotes,ReturnLocation,ReturnByDate,IsReturned,ReturnedAt PurchaseReceiptFileName,PurchaseReceiptPath,ReturnReceiptFileName,ReturnReceiptPath")] Item item, IFormFile? purchaseReceiptFile)
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
            item.ReturnReceiptFileName = existingItem.ReturnReceiptFileName;
            item.ReturnReceiptPath = existingItem.ReturnReceiptPath;

            if (purchaseReceiptFile != null)
            {
                var savedFile = await SaveUploadedFileAsync(purchaseReceiptFile, "purchases");
                if (savedFile.HasValue)
                {
                    item.PurchaseReceiptFileName = savedFile.Value.fileName;
                    item.PurchaseReceiptPath = savedFile.Value.relativePath;
                }
            }

            if (ModelState.IsValid)
            {
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
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Projects", new { id = item.ProjectId });
            }
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", item.ProjectId);
            return View(item);
        }

        // GET: Items/Delete/5
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

            item.IsReturnRequired = true;

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnInfo(int id, [Bind("Id,ProjectId,Name,ItemNumber,Category,Description,EstimatedCost,ActualCost,Status,ImageUrl,IsReturnRequired,ReturnNotes,ReturnLocation,ReturnByDate,IsReturned,ReturnedAt,PurchaseReceiptFileName,PurchaseReceiptPath,ReturnReceiptFileName,ReturnReceiptPath")] Item item, IFormFile? returnReceiptFile)
        {
            if (id != item.Id)
            {
                return NotFound();
            }

            var existingItem = await _context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
            if (existingItem == null)
            {
                return NotFound();
            }

            item.PurchaseReceiptFileName = existingItem.PurchaseReceiptFileName;
            item.PurchaseReceiptPath = existingItem.PurchaseReceiptPath;
            item.ReturnReceiptFileName = existingItem.ReturnReceiptFileName;
            item.ReturnReceiptPath = existingItem.ReturnReceiptPath;

            item.IsReturnRequired = true;

            if (returnReceiptFile != null)
            {
                var savedFile = await SaveUploadedFileAsync(returnReceiptFile, "returns");
                if (savedFile.HasValue)
                {
                    item.ReturnReceiptFileName = savedFile.Value.fileName;
                    item.ReturnReceiptPath = savedFile.Value.relativePath;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    item.IsReturnRequired = true;
                    _context.Update(item);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Items.Any(e => e.Id == item.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction("Details", "Projects", new { id = item.ProjectId });
            }

            item.Project = await _context.Projects.FindAsync(item.ProjectId);

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReturned(int id)
        {
            var item = await _context.Items.FindAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            item.IsReturned = true;
            item.ReturnedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = item.Id });
        }
    }
}
