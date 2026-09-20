using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenERP.Web.Areas.Logistics.Controllers;

[Authorize]
[Area("Logistics")]
public class WarehouseOperationsController : Controller
{
	public IActionResult Inventory()
	{
		return ShowWarehouseFeature("库存查询", "用于查询物料、仓库、库位、批次维度的库存余额。");
	}

	public IActionResult ProductInventory()
	{
		return ShowWarehouseFeature("产品库存", "用于查询成品、半成品等产品类物料的库存余额。");
	}

	public IActionResult MaterialInventory()
	{
		return ShowWarehouseFeature("材料库存", "用于查询生产、维修或仓储领用材料的库存余额。");
	}

	public IActionResult AuxiliaryMaterialInventory()
	{
		return ShowWarehouseFeature("辅料库存", "用于查询包装、消耗品等辅助物料的库存余额。");
	}

	public IActionResult Inbounds()
	{
		return ShowWarehouseFeature("入库管理", "用于维护采购入库、生产入库和其他入库业务。");
	}

	public IActionResult ReceivingNotices()
	{
		return ShowWarehouseFeature("收货通知", "用于维护供应商送货、到货预告和待收货通知。");
	}

	public IActionResult InboundVerifications()
	{
		return ShowWarehouseFeature("入库核实", "用于核对到货数量、物料信息、仓库库位和单据来源。");
	}

	public IActionResult InboundIqc()
	{
		return ShowWarehouseFeature("入库IQC", "用于记录入库质量检验、判定结果和异常处理。");
	}

	public IActionResult InboundOrders()
	{
		return ShowWarehouseFeature("入库单", "用于维护正式入库单据、入库明细和确认状态。");
	}

	public IActionResult InboundRecords()
	{
		return ShowWarehouseFeature("入库记录", "用于查询已完成入库的历史记录和追溯信息。");
	}

	public IActionResult Outbounds()
	{
		return ShowWarehouseFeature("出库管理", "用于维护销售出库、领用出库和其他出库业务。");
	}

	public IActionResult ShippingNotices()
	{
		return ShowWarehouseFeature("出货通知", "用于维护客户出货计划、出库预告和待发货通知。");
	}

	public IActionResult OutboundVerifications()
	{
		return ShowWarehouseFeature("出库核实", "用于核对出库数量、物料信息、仓库库位和单据来源。");
	}

	public IActionResult OutboundOrders()
	{
		return ShowWarehouseFeature("出库单", "用于维护正式出库单据、出库明细和确认状态。");
	}

	public IActionResult OutboundRecords()
	{
		return ShowWarehouseFeature("出库记录", "用于查询已完成出库的历史记录和追溯信息。");
	}

	public IActionResult Requisitions()
	{
		return ShowWarehouseFeature("领料单", "用于维护生产、维修或部门领用物料的单据。");
	}

	public IActionResult Returns()
	{
		return ShowWarehouseFeature("退货单", "用于维护采购退货、销售退回或内部退料单据。");
	}

	public IActionResult Transfers()
	{
		return ShowWarehouseFeature("调拨单", "用于维护跨仓库或跨组织的物料调拨单据。");
	}

	public IActionResult Replenishments()
	{
		return ShowWarehouseFeature("补货单", "用于维护库存补货建议、补货申请和补货执行记录。");
	}

	public IActionResult Relocations()
	{
		return ShowWarehouseFeature("转位单", "用于维护同一仓库内不同库位之间的物料转移。");
	}

	public IActionResult Adjustments()
	{
		return ShowWarehouseFeature("调整单", "用于维护库存数量、状态或批次差异的调整记录。");
	}

	public IActionResult Stocktakes()
	{
		return ShowWarehouseFeature("盘点管理", "用于维护库存盘点计划、盘点记录和盈亏处理。");
	}

	public IActionResult StocktakeOrders()
	{
		return ShowWarehouseFeature("盘点单", "用于维护库存盘点单据、盘点明细和盈亏确认。");
	}

	public IActionResult CycleCountPlans()
	{
		return ShowWarehouseFeature("周期盘点计划", "用于维护定期或循环盘点计划、盘点范围和执行安排。");
	}

	public IActionResult Batches()
	{
		return ShowWarehouseFeature("批次管理", "用于维护物料批次、效期、来源和追溯信息。");
	}

	public IActionResult RfidRecords()
	{
		return ShowWarehouseFeature("RFID记录", "用于查询和追溯射频识别标签的读写、出入库和库存流转记录。");
	}

	private IActionResult ShowWarehouseFeature(string featureName, string featureDescription)
	{
		base.ViewData["Title"] = featureName;
		base.ViewData["FeatureName"] = featureName;
		base.ViewData["FeatureDescription"] = featureDescription;
		return View("Index");
	}
}
