using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenERP.Web.Areas.Sales.Controllers;

[Authorize]
[Area("Sales")]
public class SalesManagementController : Controller
{
	public IActionResult Quotations()
	{
		return ShowSalesFeature("销售报价", "用于维护客户报价、价格确认和报价跟进记录。");
	}

	public IActionResult Deliveries()
	{
		return ShowSalesFeature("销售送货", "用于维护销售出库、送货安排和客户签收记录。");
	}

	public IActionResult Invoices()
	{
		return ShowSalesFeature("销售发票", "用于维护销售发票登记、开票状态和订单关联。");
	}

	public IActionResult Receipts()
	{
		return ShowSalesFeature("销售收款", "用于记录客户收款、核销和收款进度。");
	}

	public IActionResult AdvanceReceipts()
	{
		return ShowSalesFeature("预收款", "用于管理客户预收款登记、余额和后续核销。");
	}

	public IActionResult Returns()
	{
		return ShowSalesFeature("销售退换", "用于记录销售退货、换货和客户处理结果。");
	}

	public IActionResult RecurringInvoices()
	{
		return ShowSalesFeature("定期发票", "用于维护周期性开票计划、执行记录和到期提醒。");
	}

	private IActionResult ShowSalesFeature(string featureName, string featureDescription)
	{
		base.ViewData["Title"] = featureName;
		base.ViewData["FeatureName"] = featureName;
		base.ViewData["FeatureDescription"] = featureDescription;
		return View("Index");
	}
}
