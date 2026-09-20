using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenERP.Web.Areas.Transport.Controllers;

[Authorize]
[Area("Transport")]
public class TransportManagementController : Controller
{
	public IActionResult Drivers()
	{
		return ShowTransportFeature("司机资料", "用于维护司机档案、联系方式、驾驶证件、准驾车型、在职状态和服务记录。");
	}

	public IActionResult Dispatches()
	{
		return ShowTransportFeature("运输排车", "用于维护运输任务排车、车辆安排、司机安排、运输路线、计划时间和执行状态。");
	}

	public IActionResult Costs()
	{
		return ShowTransportFeature("运输费用", "用于维护运输费用登记、油费路费、人工费用、客户计费、成本归集和结算状态。");
	}

	private IActionResult ShowTransportFeature(string featureName, string featureDescription)
	{
		base.ViewData["Title"] = featureName;
		base.ViewData["FeatureName"] = featureName;
		base.ViewData["FeatureDescription"] = featureDescription;
		return View("Index");
	}
}
