/*
 * File: OpenERP.Web/Areas/CRM/Controllers/OpportunitiesController.cs
 * Description: Controller that handles HTTP actions for OpportunitiesController.
 */

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpenERP.CRM.Data;
using OpenERP.CRM.Models.Entities;

namespace OpenERP.Web.Areas.CRM.Controllers
{
    [Area("CRM")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class OpportunitiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OpportunitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Opportunities.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,AccountName,Stage,Amount,LeadId")] Opportunity opportunity)
        {
            if (ModelState.IsValid)
            {
                _context.Add(opportunity);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(opportunity);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var opportunity = await _context.Opportunities.FindAsync(id);
            if (opportunity == null) return NotFound();
            return View(opportunity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,AccountName,Stage,Amount,LeadId,CreatedAt,UpdatedAt,IsDeleted")] Opportunity opportunity)
        {
            if (id != opportunity.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(opportunity);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Opportunities.Any(e => e.Id == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(opportunity);
        }
    }
}

