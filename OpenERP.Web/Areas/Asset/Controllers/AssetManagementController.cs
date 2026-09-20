using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenERP.Web.Areas.Asset.Controllers;

[Authorize]
[Area("Asset")]
public class AssetManagementController : Controller
{
	public IActionResult PreBorrowOrders()
	{
		return ShowAssetFeature("预借单", "用于维护资产预借申请、申请部门、申请人、预计借用日期、审批状态和备注资料。");
	}

	public IActionResult BorrowOrders()
	{
		return ShowAssetFeature("借用单", "用于维护资产借用登记、借用人、借用日期、预计归还日期、签收状态和关联预借单。");
	}

	public IActionResult ReturnOrders()
	{
		return ShowAssetFeature("退还单", "用于维护资产退还登记、退还验收、资产状态变更、损耗差异和处理结果。");
	}

	public IActionResult Stocktakes()
	{
		return ShowAssetFeature("资产盘点", "用于维护资产盘点计划、盘点范围、实盘数量、盘盈盘亏、差异原因和确认结果。");
	}

	public IActionResult Depreciations()
	{
		return ShowAssetFeature("资产折旧", "用于维护资产折旧规则、折旧期间、折旧金额、净值计算、状态确认和账务归集。");
	}

	public IActionResult LossReports()
	{
		return ShowAssetFeature("资产报损", "用于维护资产损坏登记、报损原因、损失金额、审批状态、处置方式和附件资料。");
	}

	private IActionResult ShowAssetFeature(string featureName, string featureDescription)
	{
		base.ViewData["Title"] = featureName;
		base.ViewData["FeatureName"] = featureName;
		base.ViewData["FeatureDescription"] = featureDescription;
		return View("Index");
	}
}
