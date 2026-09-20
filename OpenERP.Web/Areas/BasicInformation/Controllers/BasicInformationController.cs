using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenERP.Web.Areas.BasicInformation.Controllers;

[Authorize]
[Area("BasicInformation")]
public class BasicInformationController : Controller
{
	public IActionResult Index()
	{
		return ShowBasicInformationFeature("基本资料", "用于集中维护客户友商、供应商厂商、仓库车间、工程项目和常用基础档案。");
	}

	public IActionResult Customers()
	{
		return ShowBasicInformationFeature("客户/友商资料", "用于维护客户资料、友商资料、联系人、往来信息、信用资料和业务关系记录。");
	}

	public IActionResult Suppliers()
	{
		return ShowBasicInformationFeature("供应商/厂商资料", "用于维护供应商资料、厂商资料、联系人、供货范围、结算方式和合作状态。");
	}

	public IActionResult Warehouses()
	{
		return ShowBasicInformationFeature("仓库/车间资料", "用于维护仓库资料、车间资料、库位区域、负责人、地址位置和启停状态。");
	}

	public IActionResult Projects()
	{
		return ShowBasicInformationFeature("工程项目资料", "用于维护工程项目档案、项目编号、项目名称、项目阶段、负责人和项目状态。");
	}

	private IActionResult ShowBasicInformationFeature(string featureName, string featureDescription)
	{
		base.ViewData["Title"] = featureName;
		base.ViewData["FeatureName"] = featureName;
		base.ViewData["FeatureDescription"] = featureDescription;
		return View("Index");
	}
}
