/*
 * File: OpenERP.Web/Areas/Asset/Controllers/MaintenanceRecordsController.cs
 * Description: Controller that handles HTTP actions for MaintenanceRecordsController.
 */

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpenERP.Asset.Data;
using OpenERP.Asset.Models.Entities;

namespace OpenERP.Web.Areas.Asset.Controllers
{
    [Area("Asset")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class MaintenanceRecordsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MaintenanceRecordsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var records = _context.MaintenanceRecords.Include(m => m.Asset);
            return View(await records.ToListAsync());
        }

        public IActionResult Create()
        {
            ViewData["AssetId"] = new SelectList(_context.Assets, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Description,MaintenanceDate,Cost,AssetId")] MaintenanceRecord record)
        {
            if (ModelState.IsValid)
            {
                _context.Add(record);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AssetId"] = new SelectList(_context.Assets, "Id", "Name", record.AssetId);
            return View(record);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var record = await _context.MaintenanceRecords.FindAsync(id);
            if (record == null) return NotFound();
            ViewData["AssetId"] = new SelectList(_context.Assets, "Id", "Name", record.AssetId);
            return View(record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Description,MaintenanceDate,Cost,AssetId,CreatedAt,UpdatedAt,IsDeleted")] MaintenanceRecord record)
        {
            if (id != record.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(record);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.MaintenanceRecords.Any(e => e.Id == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AssetId"] = new SelectList(_context.Assets, "Id", "Name", record.AssetId);
            return View(record);
        }
    }
}

