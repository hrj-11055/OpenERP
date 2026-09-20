/*
 * File: OpenERP.Web/Areas/CRM/Controllers/LeadsController.cs
 * Description: Controller that handles HTTP actions for LeadsController.
 */

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenERP.CRM.Data;
using OpenERP.CRM.Models.Entities;

namespace OpenERP.Web.Areas.CRM.Controllers
{
    [Area("CRM")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class LeadsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LeadsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Leads.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Company,Email,Phone,Status")] Lead lead)
        {
            if (ModelState.IsValid)
            {
                _context.Add(lead);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(lead);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var lead = await _context.Leads.FindAsync(id);
            if (lead == null) return NotFound();
            return View(lead);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Company,Email,Phone,Status,CreatedAt,UpdatedAt,IsDeleted")] Lead lead)
        {
            if (id != lead.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(lead);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Leads.Any(e => e.Id == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(lead);
        }
    }
}

