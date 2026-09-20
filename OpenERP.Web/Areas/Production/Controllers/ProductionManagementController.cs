using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenERP.Web.Areas.Production.Controllers;

[Authorize]
[Area("Production")]
public class ProductionManagementController : Controller
{
	public IActionResult Equipment()
	{
		return ShowProductionFeature("生产设备", "用于维护生产设备资料、设备状态、设备能力和使用记录。");
	}

	public IActionResult Processes()
	{
		return ShowProductionFeature("工序管理", "用于维护生产工序、工艺路线、工序顺序和标准工时。");
	}

	public IActionResult SampleWorkOrders()
	{
		return ShowProductionFeature("样板工单", "用于维护样板生产工单、样板需求、试产记录和确认结果。");
	}

	public IActionResult SchedulingPlans()
	{
		return ShowProductionFeature("排产计划", "用于维护生产排程、设备负荷、计划开始结束时间和排产状态。");
	}

	public IActionResult WorkpieceEntries()
	{
		return ShowProductionFeature("工件录入", "用于登记工件编号、来源工单、加工状态和生产流转信息。");
	}

	public IActionResult Progress()
	{
		return ShowProductionFeature("生产进度", "用于跟踪生产工单进度、完工数量、异常状态和进度汇报。");
	}

	public IActionResult FinishedProductDisassembly()
	{
		return ShowProductionFeature("成品拆解", "用于维护成品拆解单据、拆解明细、回收物料和库存影响。");
	}

	public IActionResult FinishedProductAssembly()
	{
		return ShowProductionFeature("成品组装", "用于维护成品组装单据、组件投入、成品产出和确认状态。");
	}

	public IActionResult OutsourcedProcessing()
	{
		return ShowProductionFeature("外发加工", "用于维护委外加工安排、外发数量、厂商交期和回厂记录。");
	}

	private IActionResult ShowProductionFeature(string featureName, string featureDescription)
	{
		base.ViewData["Title"] = featureName;
		base.ViewData["FeatureName"] = featureName;
		base.ViewData["FeatureDescription"] = featureDescription;
		return View("Index");
	}
}
