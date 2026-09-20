/*
 * File: OpenERP.Web/Areas/Logistics/Controllers/CarriersController.cs
 * Description: Controller that handles HTTP actions for CarriersController.
 */

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenERP.Logistics.Data;
using OpenERP.Logistics.Models.Entities;

namespace OpenERP.Web.Areas.Logistics.Controllers
{
    [Area("Logistics")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class CarriersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CarriersController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var carriers = await _context.Carriers.ToListAsync();
            return View(carriers);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Carrier carrier)
        {
            if (ModelState.IsValid)
            {
                _context.Add(carrier);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(carrier);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var carrier = await _context.Carriers.FindAsync(id);
            if (carrier == null) return NotFound();
            return View(carrier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Carrier carrier)
        {
            if (id != carrier.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(carrier);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(carrier);
        }
    }
}

