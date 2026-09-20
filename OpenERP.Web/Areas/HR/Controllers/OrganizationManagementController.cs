using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenERP.Web.Areas.HR.Controllers;

[Authorize]
[Area("HR")]
public class OrganizationManagementController : Controller
{
	public IActionResult EmployeeTraining()
	{
		return ShowOrganizationFeature("员工培训", "用于维护员工培训计划、培训记录、培训结果、证书信息和关联资料。");
	}

	public IActionResult PersonnelChanges()
	{
		return ShowOrganizationFeature("人事变更", "用于维护员工入职、转正、调岗、离职、薪酬调整和其他人事异动记录。");
	}

	public IActionResult Holidays()
	{
		return ShowOrganizationFeature("节日管理", "用于维护公司节假日、调休安排、工作日历和考勤核算使用的假期规则。");
	}

	public IActionResult Attendance()
	{
		return ShowOrganizationFeature("考勤管理", "用于维护班次规则、打卡记录、请假记录、加班记录和月度考勤汇总。");
	}

	public IActionResult ShiftSettings()
	{
		return ShowOrganizationFeature("班次设置", "用于维护上下班时段、休息时间、迟到早退规则、弹性班次和适用组织范围。");
	}

	public IActionResult AttendanceScheduling()
	{
		return ShowOrganizationFeature("考勤排班", "用于维护员工排班计划、轮班周期、临时调班和排班生效范围。");
	}

	public IActionResult AttendanceRecords()
	{
		return ShowOrganizationFeature("出勤记录", "用于查询和维护员工打卡记录、出勤状态、异常说明和补卡核对结果。");
	}

	public IActionResult LeaveRegistrations()
	{
		return ShowOrganizationFeature("请假登记", "用于登记员工请假申请、请假类型、开始结束时间、请假原因和附件资料。");
	}

	public IActionResult LeaveApprovals()
	{
		return ShowOrganizationFeature("请假审批", "用于处理请假审批、退回、撤销、审批意见和审批状态追踪。");
	}

	public IActionResult FieldWorkRegistrations()
	{
		return ShowOrganizationFeature("外勤登记", "用于登记外出办公、客户拜访、外勤地点、外勤时间和审批状态。");
	}

	public IActionResult AttendanceDevices()
	{
		return ShowOrganizationFeature("考勤机管理", "用于维护考勤设备、设备编号、安装位置、同步状态和数据采集规则。");
	}

	public IActionResult AttendanceAdjustments()
	{
		return ShowOrganizationFeature("应补应扣", "用于维护因迟到、早退、缺勤、加班或补贴产生的应补应扣明细。");
	}

	public IActionResult AttendanceStatistics()
	{
		return ShowOrganizationFeature("考勤统计", "用于统计员工出勤、迟到、早退、缺勤、请假、外勤和应补应扣结果。");
	}

	private IActionResult ShowOrganizationFeature(string featureName, string featureDescription)
	{
		base.ViewData["Title"] = featureName;
		base.ViewData["FeatureName"] = featureName;
		base.ViewData["FeatureDescription"] = featureDescription;
		return View("Index");
	}
}
