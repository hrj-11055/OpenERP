using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpenERP.HR.Models.Entities;
using OpenERP.Production.Data;
using OpenERP.Production.Models.Entities;
using OpenERP.Web.Data.HR;

namespace OpenERP.Web.Areas.Production.Controllers;

[Area("Production")]
public class ProductionOrdersController : Controller
{
	private readonly ApplicationDbContext _context;

	private readonly IHrRepository _hrRepository;

	private const string ProductionOrderDocumentTypeCode = "PRODUCTION_ORDER";

	public ProductionOrdersController(ApplicationDbContext context, IHrRepository hrRepository)
	{
		_context = context;
		_hrRepository = hrRepository;
	}

	public async Task<IActionResult> Index()
	{
		return View(await _context.ProductionOrders.Include((ProductionOrder p) => p.WorkCenter).ToListAsync());
	}

	public IActionResult Create()
	{
		base.ViewData["WorkCenterId"] = new SelectList(_context.WorkCenters, "Id", "Name");
		return View(new ProductionOrder());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create([Bind(new string[] { "OrderNumber,OrderDate,WorkCenterId,Status,ProductName,PlannedQuantity,StartDate,EndDate,TotalQuantity" })] ProductionOrder po)
	{
		base.ModelState.Remove("OrderNumber");
		string generatedOrderNumber = await TryGenerateOrderNumberAsync(po.OrderDate);
		if (generatedOrderNumber == null)
		{
			base.ViewData["WorkCenterId"] = new SelectList(_context.WorkCenters, "Id", "Name", po.WorkCenterId);
			return View(po);
		}
		po.OrderNumber = generatedOrderNumber;
		if (base.ModelState.IsValid)
		{
			_context.Add(po);
			await _context.SaveChangesAsync();
			return RedirectToAction("Index");
		}
		base.ViewData["WorkCenterId"] = new SelectList(_context.WorkCenters, "Id", "Name", po.WorkCenterId);
		return View(po);
	}

	private async Task<string?> TryGenerateOrderNumberAsync(DateTime orderDate)
	{
		string companyCode = base.User.FindFirst("erp:company")?.Value;
		if (string.IsNullOrWhiteSpace(companyCode))
		{
			base.ModelState.AddModelError("OrderNumber", "当前未选择所属公司，无法自动生成生产工单单号。");
			return null;
		}
		CompanyOrganization organization = await _hrRepository.GetCompanyOrganizationByCodeAsync(companyCode);
		if (organization == null)
		{
			base.ModelState.AddModelError("OrderNumber", "当前所属公司无效，无法自动生成生产工单单号。");
			return null;
		}
		return await _hrRepository.GenerateDocumentNumberAsync(organization.Id, "PRODUCTION_ORDER", orderDate);
	}
}
