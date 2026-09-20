/*
 * File: OpenERP.Web/Areas/Logistics/Controllers/ShipmentsController.cs
 * Description: Controller that handles HTTP actions for ShipmentsController.
 */

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpenERP.Logistics.Data;
using OpenERP.Logistics.Models.Entities;

namespace OpenERP.Web.Areas.Logistics.Controllers
{
    [Area("Logistics")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class ShipmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ShipmentsController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var list = await _context.Shipments.Include(p => p.Carrier).ToListAsync();
            return View(list);
        }

        public IActionResult Create()
        {
            ViewData["CarrierId"] = new SelectList(_context.Carriers, "Id", "Name");
            return View(new Shipment());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ShipmentNumber,ShipmentDate,CarrierId,Status,TotalWeight")] Shipment shipment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(shipment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CarrierId"] = new SelectList(_context.Carriers, "Id", "Name", shipment.CarrierId);
            return View(shipment);
        }
    }
}

