/*
 * File: OpenERP.Web/Areas/Production/Controllers/WorkCentersController.cs
 * Description: Controller that handles HTTP actions for WorkCentersController.
 */

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenERP.Production.Data;
using OpenERP.Production.Models.Entities;

namespace OpenERP.Web.Areas.Production.Controllers
{
    [Area("Production")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class WorkCentersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public WorkCentersController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var workCenters = await _context.WorkCenters.ToListAsync();
            return View(workCenters);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkCenter wc)
        {
            if (ModelState.IsValid)
            {
                _context.Add(wc);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(wc);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var wc = await _context.WorkCenters.FindAsync(id);
            if (wc == null) return NotFound();
            return View(wc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WorkCenter wc)
        {
            if (id != wc.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(wc);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(wc);
        }
    }
}

