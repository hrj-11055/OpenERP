/*
 * File: OpenERP.Web/Areas/Office/Controllers/MeetingsController.cs
 * Description: Controller that handles HTTP actions for MeetingsController.
 */

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenERP.Office.Data;
using OpenERP.Office.Models.Entities;

namespace OpenERP.Web.Areas.Office.Controllers
{
    [Area("Office")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class MeetingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MeetingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Meetings.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,StartTime,EndTime,Location,Agenda")] Meeting meeting)
        {
            if (ModelState.IsValid)
            {
                _context.Add(meeting);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(meeting);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var meeting = await _context.Meetings.FindAsync(id);
            if (meeting == null) return NotFound();
            return View(meeting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,StartTime,EndTime,Location,Agenda,CreatedAt,UpdatedAt,IsDeleted")] Meeting meeting)
        {
            if (id != meeting.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(meeting);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Meetings.Any(e => e.Id == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(meeting);
        }
    }
}

