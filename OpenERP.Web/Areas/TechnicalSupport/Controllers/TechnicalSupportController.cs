using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenERP.Web.Areas.TechnicalSupport.Controllers;

[Authorize]
[Area("TechnicalSupport")]
public class TechnicalSupportController : Controller
{
	public IActionResult Index()
	{
		return ShowTechnicalSupportFeature("技术支持", "用于维护在线帮助、下载中心、在线支持、联络信息、系统版本、在线更新、更新日志和API管理。");
	}

	public IActionResult OnlineHelp()
	{
		return ShowTechnicalSupportFeature("在线帮助", "用于维护帮助文档、常见问题、操作指引、业务说明和用户自助查询内容。");
	}

	public IActionResult DownloadCenter()
	{
		return ShowTechnicalSupportFeature("下载中心", "用于维护系统安装包、升级包、导入模板、操作手册、技术资料和下载记录。");
	}

	public IActionResult OnlineSupport()
	{
		return ShowTechnicalSupportFeature("在线支持", "用于提交在线支持请求、问题描述、处理人员、处理状态、沟通记录和满意度反馈。");
	}

	public IActionResult ContactUs()
	{
		return ShowTechnicalSupportFeature("联络我们", "用于维护技术支持电话、邮箱、联系人、服务时间、服务地址和紧急联络方式。");
	}

	public IActionResult SystemVersion()
	{
		return ShowTechnicalSupportFeature("系统版本", "用于查看系统版本号、构建时间、模块版本、授权状态、运行环境和部署信息。");
	}

	public IActionResult OnlineUpdates()
	{
		return ShowTechnicalSupportFeature("在线更新", "用于检测在线更新、更新包下载、更新安装、更新状态、失败重试和更新提醒。");
	}

	public IActionResult UpdateLogs()
	{
		return ShowTechnicalSupportFeature("更新日志", "用于查看版本更新内容、新增功能、问题修复、兼容说明和发布日期记录。");
	}

	public IActionResult ApiManagement()
	{
		return ShowTechnicalSupportFeature("API管理", "用于维护API接口资料、访问密钥、授权范围、调用限制、接口状态和调用日志。");
	}

	private IActionResult ShowTechnicalSupportFeature(string featureName, string featureDescription)
	{
		base.ViewData["Title"] = featureName;
		base.ViewData["FeatureName"] = featureName;
		base.ViewData["FeatureDescription"] = featureDescription;
		return View("Index");
	}
}
