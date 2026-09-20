using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpenERP.HR.Models.Entities;
using OpenERP.Sales.Data;
using OpenERP.Sales.Models.Entities;
using OpenERP.Web.Data.HR;

namespace OpenERP.Web.Areas.Sales.Controllers;

[Authorize]
[Area("Sales")]
public class SalesOrdersController : Controller
{
	private readonly ApplicationDbContext _context;

	private readonly IHrRepository _hrRepository;

	private const string SalesOrderDocumentTypeCode = "SALES_ORDER";

	public SalesOrdersController(ApplicationDbContext context, IHrRepository hrRepository)
	{
		_context = context;
		_hrRepository = hrRepository;
	}

	public async Task<IActionResult> Index()
	{
		return View(await _context.SalesOrders.Include((SalesOrder p) => p.Customer).ToListAsync());
	}

	public IActionResult Create()
	{
		base.ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name");
		return View(new SalesOrder());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create([Bind(new string[] { "OrderDate,CustomerId,TotalAmount" })] SalesOrder so)
	{
		base.ModelState.Remove("OrderNumber");
		// 先做模型校验,校验通过后再消耗单号流水（避免校验失败导致断号）。
		if (!base.ModelState.IsValid)
		{
			base.ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name", so.CustomerId);
			return View(so);
		}

		// 单据状态由服务端控制（新建一律为草稿,不接收表单提交,防止伪造已过账单据）。
		so.Status = "Draft";
		string generatedOrderNumber = await TryGenerateOrderNumberAsync(so.OrderDate);
		if (generatedOrderNumber == null)
		{
			base.ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name", so.CustomerId);
			return View(so);
		}
		so.OrderNumber = generatedOrderNumber;
		_context.Add(so);
		await _context.SaveChangesAsync();
		return RedirectToAction("Index");
	}

	private async Task<string?> TryGenerateOrderNumberAsync(DateTime orderDate)
	{
		string companyCode = base.User.FindFirst("erp:company")?.Value;
		if (string.IsNullOrWhiteSpace(companyCode))
		{
			base.ModelState.AddModelError("OrderNumber", "当前未选择所属公司，无法自动生成销售订单单号。");
			return null;
		}
		CompanyOrganization organization = await _hrRepository.GetCompanyOrganizationByCodeAsync(companyCode);
		if (organization == null)
		{
			base.ModelState.AddModelError("OrderNumber", "当前所属公司无效，无法自动生成销售订单单号。");
			return null;
		}
		return await _hrRepository.GenerateDocumentNumberAsync(organization.Id, "SALES_ORDER", orderDate);
	}
}
