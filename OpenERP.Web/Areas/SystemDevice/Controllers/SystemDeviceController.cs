using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenERP.Web.Areas.SystemDevice.Controllers;

[Authorize]
[Area("SystemDevice")]
public class SystemDeviceController : Controller
{
	public IActionResult Index()
	{
		return ShowSystemDeviceFeature("系统设备", "用于维护系统终端、采集设备、连接状态、维护记录和设备配置。");
	}

	public IActionResult RfidReaders()
	{
		return ShowSystemDeviceFeature("RFID Reader", "用于维护RFID Reader设备档案、设备编号、连接地址、运行状态、读写参数和维护记录。");
	}

	public IActionResult CameraDevices()
	{
		return ShowSystemDeviceFeature("镜头设备", "用于维护镜头设备档案、设备位置、采集参数、在线状态、异常提醒和维护记录。");
	}

	public IActionResult GpioDevices()
	{
		return ShowSystemDeviceFeature("GPIO设备", "用于维护GPIO设备档案、接口点位、输入状态、输出控制、连接状态和维护记录。");
	}

	private IActionResult ShowSystemDeviceFeature(string featureName, string featureDescription)
	{
		base.ViewData["Title"] = featureName;
		base.ViewData["FeatureName"] = featureName;
		base.ViewData["FeatureDescription"] = featureDescription;
		return View("Index");
	}
}
