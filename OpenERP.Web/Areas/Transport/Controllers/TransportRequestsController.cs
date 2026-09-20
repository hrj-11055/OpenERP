/*
 * File: OpenERP.Web/Areas/Transport/Controllers/TransportRequestsController.cs
 * Description: Controller that handles HTTP actions for TransportRequestsController.
 */

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpenERP.Transport.Data;
using OpenERP.Transport.Models.Entities;

namespace OpenERP.Web.Areas.Transport.Controllers
{
    [Area("Transport")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class TransportRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransportRequestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var requests = _context.TransportRequests.Include(t => t.Vehicle);
            return View(await requests.ToListAsync());
        }

        public IActionResult Create()
        {
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Status,VehicleId")] TransportRequest request)
        {
            if (ModelState.IsValid)
            {
                _context.Add(request);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "Id", "Name", request.VehicleId);
            return View(request);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var request = await _context.TransportRequests.FindAsync(id);
            if (request == null) return NotFound();
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "Id", "Name", request.VehicleId);
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Status,VehicleId,CreatedAt,UpdatedAt,IsDeleted")] TransportRequest request)
        {
            if (id != request.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(request);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.TransportRequests.Any(e => e.Id == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "Id", "Name", request.VehicleId);
            return View(request);
        }
    }
}

