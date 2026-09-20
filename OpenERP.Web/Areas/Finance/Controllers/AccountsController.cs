/*
 * File: OpenERP.Web/Areas/Finance/Controllers/AccountsController.cs
 * Description: Controller that handles HTTP actions for AccountsController.
 */

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenERP.Finance.Data;
using OpenERP.Finance.Models.Entities;

namespace OpenERP.Web.Areas.Finance.Controllers
{
    [Area("Finance")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class AccountsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AccountsController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var accounts = await _context.Accounts.ToListAsync();
            return View(accounts);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Account account)
        {
            if (ModelState.IsValid)
            {
                _context.Add(account);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(account);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return NotFound();
            return View(account);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Account account)
        {
            if (id != account.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(account);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(account);
        }
    }
}

