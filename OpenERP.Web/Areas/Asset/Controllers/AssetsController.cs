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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,AssetTag,Status,PurchaseDate,PurchaseCost")] AssetEntity asset)
        {
            if (id != asset.Id) return NotFound();
            // 加载现有实体后仅拷贝业务字段：审计字段（CreatedAt/CreatedBy）与软删除标记不允许被表单覆盖。
            var existing = await _context.Assets.FindAsync(id);
            if (existing == null) return NotFound();
            if (ModelState.IsValid)
            {
                existing.Name = asset.Name;
                existing.AssetTag = asset.AssetTag;
                existing.Status = asset.Status;
                existing.PurchaseDate = asset.PurchaseDate;
                existing.PurchaseCost = asset.PurchaseCost;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(asset);
        }
    }
}

