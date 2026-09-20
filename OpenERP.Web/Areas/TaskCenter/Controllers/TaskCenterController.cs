using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenERP.Web.Areas.TaskCenter.Controllers;

[Authorize]
[Area("TaskCenter")]
public class TaskCenterController : Controller
{
	public IActionResult Index()
	{
		return ShowTaskFeature("任务中心", "用于集中处理待办任务、审批提醒、流程跟进、已办记录和消息通知。");
	}

	public IActionResult MyApprovals()
	{
		return ShowTaskFeature("我的审批", "用于处理待审单据、审批提醒、审批意见、审批历史和流程跟踪。");
	}

	public IActionResult DailyTasks()
	{
		return ShowTaskFeature("日常任务", "用于维护个人日常任务、协作任务、负责人、计划时间、完成进度和提醒记录。");
	}

	public IActionResult FollowUps()
	{
		return ShowTaskFeature("跟单管理", "用于维护业务跟单、跟进节点、责任人、跟进记录、异常提醒和处理结果。");
	}

	public IActionResult ContentMessages()
	{
		return ShowTaskFeature("内容消息", "用于查看站内消息、业务通知、消息分类、发送对象、阅读状态和处理记录。");
	}

	public IActionResult Emails()
	{
		return ShowTaskFeature("收发邮件", "用于维护邮件收件、发件、草稿、附件、收件人、发送状态和业务关联记录。");
	}

	public IActionResult Announcements()
	{
		return ShowTaskFeature("系统公告", "用于维护系统公告、发布范围、生效时间、失效时间、阅读确认和置顶状态。");
	}

	private IActionResult ShowTaskFeature(string featureName, string featureDescription)
	{
		base.ViewData["Title"] = featureName;
		base.ViewData["FeatureName"] = featureName;
		base.ViewData["FeatureDescription"] = featureDescription;
		return View("Index");
	}
}
