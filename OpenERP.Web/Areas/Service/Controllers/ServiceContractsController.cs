/*
 * File: OpenERP.Web/Areas/Service/Controllers/ServiceContractsController.cs
 * Description: Controller that handles HTTP actions for ServiceContractsController.
 */

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenERP.Service.Data;
using OpenERP.Service.Models.Entities;

namespace OpenERP.Web.Areas.Service.Controllers
{
    [Area("Service")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class ServiceContractsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceContractsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.ServiceContracts.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,CustomerName,StartDate,EndDate,Status")] ServiceContract contract)
        {
            if (ModelState.IsValid)
            {
                _context.Add(contract);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(contract);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var contract = await _context.ServiceContracts.FindAsync(id);
            if (contract == null) return NotFound();
            return View(contract);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,CustomerName,StartDate,EndDate,Status,CreatedAt,UpdatedAt,IsDeleted")] ServiceContract contract)
        {
            if (id != contract.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(contract);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ServiceContracts.Any(e => e.Id == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(contract);
        }
    }
}

