using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenERP.Web.Areas.SystemManagement.Controllers;

[Authorize]
[Area("SystemManagement")]
public class SystemManagementController : Controller
{
	public IActionResult Index()
	{
		return ShowSystemManagementFeature("系统管理", "用于集中维护用户资料、角色权限、系统设置、在线用户和系统日志。");
	}

	public IActionResult Users()
	{
		return ShowSystemManagementFeature("用户资料", "用于维护用户账号、用户名称、登录状态、所属员工、所属公司和账号启停信息。");
	}

	public IActionResult Roles()
	{
		return ShowSystemManagementFeature("角色管理", "用于维护角色资料、角色权限、角色成员、授权范围和启停状态。");
	}

	public IActionResult Settings()
	{
		return ShowSystemManagementFeature("系统设置", "用于维护系统参数、功能开关、运行配置、默认规则和安全策略。");
	}

	public IActionResult OnlineUsers()
	{
		return ShowSystemManagementFeature("在线用户", "用于查看当前在线用户、登录时间、登录公司、会话状态和最后活动时间。");
	}

	public IActionResult Logs()
	{
		return ShowSystemManagementFeature("查看日志", "用于查询系统日志、操作日志、登录日志、异常日志和关键业务审计记录。");
	}

	public IActionResult LoginLogs()
	{
		return ShowSystemManagementFeature("登入日志", "用于查询用户登入、登出、失败登录、登录公司、登录地址和登录设备记录。");
	}

	public IActionResult OperationLogs()
	{
		return ShowSystemManagementFeature("操作日志", "用于查询用户操作、数据新增、数据修改、数据删除、业务审批和关键操作审计记录。");
	}

	public IActionResult SystemLogs()
	{
		return ShowSystemManagementFeature("系统日志", "用于查询系统运行、异常错误、服务启动、后台任务、接口调用和配置变更记录。");
	}

	private IActionResult ShowSystemManagementFeature(string featureName, string featureDescription)
	{
		base.ViewData["Title"] = featureName;
		base.ViewData["FeatureName"] = featureName;
		base.ViewData["FeatureDescription"] = featureDescription;
		return View("Index");
	}
}
