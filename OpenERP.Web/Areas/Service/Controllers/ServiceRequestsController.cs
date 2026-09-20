/*
 * File: OpenERP.Web/Areas/Service/Controllers/ServiceRequestsController.cs
 * Description: Controller that handles HTTP actions for ServiceRequestsController.
 */

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpenERP.Service.Data;
using OpenERP.Service.Models.Entities;

namespace OpenERP.Web.Areas.Service.Controllers
{
    [Area("Service")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class ServiceRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceRequestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var requests = _context.ServiceRequests.Include(s => s.ServiceContract);
            return View(await requests.ToListAsync());
        }

        public IActionResult Create()
        {
            ViewData["ServiceContractId"] = new SelectList(_context.ServiceContracts, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Status,ServiceContractId")] ServiceRequest request)
        {
            if (ModelState.IsValid)
            {
                _context.Add(request);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ServiceContractId"] = new SelectList(_context.ServiceContracts, "Id", "Name", request.ServiceContractId);
            return View(request);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var request = await _context.ServiceRequests.FindAsync(id);
            if (request == null) return NotFound();
            ViewData["ServiceContractId"] = new SelectList(_context.ServiceContracts, "Id", "Name", request.ServiceContractId);
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Status,ServiceContractId,CreatedAt,UpdatedAt,IsDeleted")] ServiceRequest request)
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
                    if (!_context.ServiceRequests.Any(e => e.Id == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ServiceContractId"] = new SelectList(_context.ServiceContracts, "Id", "Name", request.ServiceContractId);
            return View(request);
        }
    }
}

