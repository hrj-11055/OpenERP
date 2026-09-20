using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenERP.Web.Areas.ReportCenter.Controllers;

[Authorize]
[Area("ReportCenter")]
public class ReportCenterController : Controller
{
	public IActionResult Index()
	{
		return ShowReportFeature("报表中心", "用于集中查看经营报表、业务统计、管理分析、查询筛选和数据导出。");
	}

	public IActionResult HrReports()
	{
		return ShowReportFeature("人事报表", "用于查看员工结构、部门编制、入离职、培训、人员异动和人事档案统计。");
	}

	public IActionResult FinanceReports()
	{
		return ShowReportFeature("财务报表", "用于查看应收应付、收付款、费用、成本、利润和财务汇总分析。");
	}

	public IActionResult PurchasingReports()
	{
		return ShowReportFeature("采购报表", "用于查看采购询价、采购订单、供应商交付、采购发票和采购成本统计。");
	}

	public IActionResult SalesReports()
	{
		return ShowReportFeature("销售报表", "用于查看销售报价、销售订单、发货、开票、回款和客户销售分析。");
	}

	public IActionResult WarehouseReports()
	{
		return ShowReportFeature("仓务报表", "用于查看库存余额、出入库、调拨、盘点差异、批次和仓务作业统计。");
	}

	public IActionResult ProductionReports()
	{
		return ShowReportFeature("生产报表", "用于查看生产工单、排产计划、工序进度、产量、外发加工和生产效率统计。");
	}

	public IActionResult ServiceReports()
	{
		return ShowReportFeature("服务报表", "用于查看借出归还、维修保养、外出服务、保养合约和服务完成情况统计。");
	}

	public IActionResult AttendanceReports()
	{
		return ShowReportFeature("考勤报表", "用于查看排班、打卡、请假、外勤、异常调整和考勤汇总统计。");
	}

	public IActionResult AssetReports()
	{
		return ShowReportFeature("资产报表", "用于查看资产台账、借用退还、维护、盘点、折旧和报损统计。");
	}

	public IActionResult OfficeReports()
	{
		return ShowReportFeature("办公报表", "用于查看办公用品申请、领用、退回、报损、会议和办公任务统计。");
	}

	public IActionResult TransportReports()
	{
		return ShowReportFeature("运输报表", "用于查看车辆资料、司机资料、运输排车、运输申请和运输费用统计。");
	}

	public IActionResult CrmReports()
	{
		return ShowReportFeature("CRM报表", "用于查看客户资料、客户跟进、销售线索、商机转化和客户服务统计。");
	}

	private IActionResult ShowReportFeature(string featureName, string featureDescription)
	{
		base.ViewData["Title"] = featureName;
		base.ViewData["FeatureName"] = featureName;
		base.ViewData["FeatureDescription"] = featureDescription;
		return View("Index");
	}
}
