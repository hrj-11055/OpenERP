using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenERP.Web.Areas.CRM.Controllers;

[Authorize]
[Area("CRM")]
public class CrmManagementController : Controller
{
	public IActionResult BusinessPartners()
	{
		return ShowCrmFeature("友商资料", "用于维护友商档案、业务联系人、合作关系、资质信息和往来备注。");
	}

	public IActionResult Contracts()
	{
		return ShowCrmFeature("合约管理", "用于维护客户合约、友商合约、签约日期、履约状态、到期提醒和附件资料。");
	}

	public IActionResult CommissionRules()
	{
		return ShowCrmFeature("佣金规则", "用于维护销售佣金规则、提成比例、适用客户、适用人员和结算条件。");
	}

	public IActionResult CustomerVisits()
	{
		return ShowCrmFeature("客户拜访", "用于维护客户拜访计划、拜访记录、沟通纪要、后续跟进和关联商机。");
	}

	public IActionResult CustomerTraining()
	{
		return ShowCrmFeature("客户培训", "用于维护客户培训计划、培训主题、参训人员、培训结果和反馈记录。");
	}

	public IActionResult TechnicalSeminars()
	{
		return ShowCrmFeature("技术讲座", "用于维护技术讲座安排、讲座主题、参会客户、讲师信息和讲座资料。");
	}

	public IActionResult MassPromotions()
	{
		return ShowCrmFeature("宣传群发", "用于维护客户宣传群发任务、发送渠道、目标客户、发送状态和触达记录。");
	}

	private IActionResult ShowCrmFeature(string featureName, string featureDescription)
	{
		base.ViewData["Title"] = featureName;
		base.ViewData["FeatureName"] = featureName;
		base.ViewData["FeatureDescription"] = featureDescription;
		return View("Index");
	}
}
