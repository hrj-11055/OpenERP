using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenERP.Web.Areas.Service.Controllers;

[Authorize]
[Area("Service")]
public class ServiceManagementController : Controller
{
	public IActionResult LendingOrders()
	{
		return ShowServiceFeature("借出单", "用于维护服务物品借出登记、借出客户、借出日期、预计归还日期、签收状态和附件资料。");
	}

	public IActionResult ReturnOrders()
	{
		return ShowServiceFeature("归还单", "用于维护服务物品归还登记、归还验收、损耗差异、处理结果和关联借出单。");
	}

	public IActionResult Maintenance()
	{
		return ShowServiceFeature("维修保养", "用于维护设备维修工单、保养计划、故障描述、处理过程、费用记录和完工确认。");
	}

	public IActionResult FieldService()
	{
		return ShowServiceFeature("外出服务", "用于维护上门服务派工、服务人员、出发到达时间、客户确认、服务结果和费用资料。");
	}

	public IActionResult MaintenanceContracts()
	{
		return ShowServiceFeature("保养合约", "用于维护客户保养协议、保养周期、合约期限、服务范围、费用条款和到期提醒。");
	}

	public IActionResult MaintenanceQueries()
	{
		return ShowServiceFeature("保养查询", "用于查询保养计划、保养记录、合约执行状态、到期提醒和客户服务历史。");
	}

	private IActionResult ShowServiceFeature(string featureName, string featureDescription)
	{
		base.ViewData["Title"] = featureName;
		base.ViewData["FeatureName"] = featureName;
		base.ViewData["FeatureDescription"] = featureDescription;
		return View("Index");
	}
}
