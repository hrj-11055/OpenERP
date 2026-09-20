using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpenERP.HR.Models.Entities;
using OpenERP.Purchasing.Data;
using OpenERP.Purchasing.Models.Entities;
using OpenERP.Web.Data.HR;

namespace OpenERP.Web.Areas.Purchasing.Controllers;

[Authorize]
[Area("Purchasing")]
public class PurchaseOrdersController : Controller
{
	private readonly ApplicationDbContext _context;

	private readonly IHrRepository _hrRepository;

	private const string PurchaseOrderDocumentTypeCode = "PURCHASE_ORDER";

	public PurchaseOrdersController(ApplicationDbContext context, IHrRepository hrRepository)
	{
		_context = context;
		_hrRepository = hrRepository;
	}

	public async Task<IActionResult> Index()
	{
		return View(await _context.PurchaseOrders.Include((PurchaseOrder p) => p.Supplier).ToListAsync());
	}

	public IActionResult Create()
	{
		base.ViewData["SupplierId"] = new SelectList(_context.Suppliers, "Id", "Name");
		return View(new PurchaseOrder());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create([Bind(new string[] { "OrderNumber,OrderDate,SupplierId,Status,TotalAmount" })] PurchaseOrder po)
	{
		base.ModelState.Remove("OrderNumber");
		string generatedOrderNumber = await TryGenerateOrderNumberAsync(po.OrderDate);
		if (generatedOrderNumber == null)
		{
			base.ViewData["SupplierId"] = new SelectList(_context.Suppliers, "Id", "Name", po.SupplierId);
			return View(po);
		}
		po.OrderNumber = generatedOrderNumber;
		if (base.ModelState.IsValid)
		{
			_context.Add(po);
			await _context.SaveChangesAsync();
			return RedirectToAction("Index");
		}
		base.ViewData["SupplierId"] = new SelectList(_context.Suppliers, "Id", "Name", po.SupplierId);
		return View(po);
	}

	private async Task<string?> TryGenerateOrderNumberAsync(DateTime orderDate)
	{
		string companyCode = base.User.FindFirst("erp:company")?.Value;
		if (string.IsNullOrWhiteSpace(companyCode))
		{
			base.ModelState.AddModelError("OrderNumber", "当前未选择所属公司，无法自动生成采购订单单号。");
			return null;
		}
		CompanyOrganization organization = await _hrRepository.GetCompanyOrganizationByCodeAsync(companyCode);
		if (organization == null)
		{
			base.ModelState.AddModelError("OrderNumber", "当前所属公司无效，无法自动生成采购订单单号。");
			return null;
		}
		return await _hrRepository.GenerateDocumentNumberAsync(organization.Id, "PURCHASE_ORDER", orderDate);
	}
}
