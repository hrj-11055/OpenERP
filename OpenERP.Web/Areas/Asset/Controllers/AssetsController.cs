/*
 * File: OpenERP.Web/Areas/Asset/Controllers/AssetsController.cs
 * Description: Controller that handles HTTP actions for AssetsController.
 */

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenERP.Asset.Data;
using AssetEntity = OpenERP.Asset.Models.Entities.Asset;

namespace OpenERP.Web.Areas.Asset.Controllers
{
    [Area("Asset")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class AssetsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AssetsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Assets.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,AssetTag,Status,PurchaseDate,PurchaseCost")] AssetEntity asset)
        {
            if (ModelState.IsValid)
            {
                _context.Add(asset);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(asset);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null) return NotFound();
            return View(asset);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,AssetTag,Status,PurchaseDate,PurchaseCost,CreatedAt,UpdatedAt,IsDeleted")] AssetEntity asset)
        {
            if (id != asset.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(asset);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Assets.Any(e => e.Id == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(asset);
        }
    }
}

