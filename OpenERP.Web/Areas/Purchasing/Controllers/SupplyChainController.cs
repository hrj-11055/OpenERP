using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenERP.Web.Areas.Purchasing.Controllers;

[Authorize]
[Area("Purchasing")]
public class SupplyChainController : Controller
{
	public IActionResult Contracts()
	{
		return ShowSupplyChainFeature("合约管理", "用于维护供应商合约、采购协议和框架合作条款。");
	}

	public IActionResult Inquiries()
	{
		return ShowSupplyChainFeature("采购询价", "用于记录采购询价、比价和供应商报价跟进。");
	}

	public IActionResult Invoices()
	{
		return ShowSupplyChainFeature("采购发票", "用于维护采购发票登记、核对和后续付款关联。");
	}

	public IActionResult Payments()
	{
		return ShowSupplyChainFeature("采购付款", "用于记录采购付款申请、付款进度和付款结果。");
	}

	public IActionResult Prepayments()
	{
		return ShowSupplyChainFeature("预付款", "用于管理采购预付款申请、核销和余额跟踪。");
	}

	public IActionResult Returns()
	{
		return ShowSupplyChainFeature("采购退换", "用于记录采购退货、换货和供应商处理结果。");
	}

	private IActionResult ShowSupplyChainFeature(string featureName, string featureDescription)
	{
		base.ViewData["Title"] = featureName;
		base.ViewData["FeatureName"] = featureName;
		base.ViewData["FeatureDescription"] = featureDescription;
		return View("Index");
	}
}
