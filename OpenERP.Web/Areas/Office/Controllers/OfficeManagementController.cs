using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenERP.Web.Areas.Office.Controllers;

[Authorize]
[Area("Office")]
public class OfficeManagementController : Controller
{
	public IActionResult Supplies()
	{
		return ShowOfficeFeature("办公用品", "用于维护办公用品档案、规格型号、库存数量、安全库存、存放位置和领用规则。");
	}

	public IActionResult ApplyOrders()
	{
		return ShowOfficeFeature("申请单", "用于维护办公用品申请、申请部门、申请人、申请明细、审批状态和需求日期。");
	}

	public IActionResult IssueOrders()
	{
		return ShowOfficeFeature("领用单", "用于维护办公用品领用登记、领用人、领用数量、发放确认、库存扣减和关联申请单。");
	}

	public IActionResult ReturnOrders()
	{
		return ShowOfficeFeature("退回单", "用于维护办公用品退回登记、退回人、退回数量、验收结果、库存回补和处理备注。");
	}

	public IActionResult LossOrders()
	{
		return ShowOfficeFeature("报损单", "用于维护办公用品报损登记、损坏原因、报损数量、审批状态、处置方式和附件资料。");
	}

	private IActionResult ShowOfficeFeature(string featureName, string featureDescription)
	{
		base.ViewData["Title"] = featureName;
		base.ViewData["FeatureName"] = featureName;
		base.ViewData["FeatureDescription"] = featureDescription;
		return View("Index");
	}
}
