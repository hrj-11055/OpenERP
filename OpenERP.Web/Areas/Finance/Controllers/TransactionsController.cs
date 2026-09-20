/*
 * File: OpenERP.Web/Areas/Finance/Controllers/TransactionsController.cs
 * Description: Controller that handles HTTP actions for TransactionsController.
 */

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpenERP.Finance.Data;
using OpenERP.Finance.Models.Entities;

namespace OpenERP.Web.Areas.Finance.Controllers
{
    [Area("Finance")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public TransactionsController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var list = await _context.Transactions.Include(t => t.Account).ToListAsync();
            return View(list);
        }

        public IActionResult Create()
        {
            ViewData["AccountId"] = new SelectList(_context.Accounts, "Id", "Name");
            return View(new Transaction());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DocNumber,DocDate,AccountId,Amount,Description,Status")] Transaction tx)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tx);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AccountId"] = new SelectList(_context.Accounts, "Id", "Name", tx.AccountId);
            return View(tx);
        }
    }
}

