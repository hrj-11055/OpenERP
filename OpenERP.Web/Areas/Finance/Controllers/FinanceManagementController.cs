using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OpenERP.Web.Areas.Finance.Controllers;

[Authorize]
[Area("Finance")]
public class FinanceManagementController : Controller
{
	public IActionResult Receipts()
	{
		return ShowFinanceFeature("收款管理", "用于维护客户收款、收款核销、收款状态和资金到账记录。");
	}

	public IActionResult AccountsReceivable()
	{
		return ShowFinanceFeature("应收账", "用于查询客户应收余额、账龄、到期款项和应收核销状态。");
	}

	public IActionResult DebitNotes()
	{
		return ShowFinanceFeature("借记单", "用于维护客户借记单、调整原因、应收金额和关联单据。");
	}

	public IActionResult AdvanceReceipts()
	{
		return ShowFinanceFeature("预收款", "用于维护客户预收款登记、余额、占用和后续核销记录。");
	}

	public IActionResult ReceiptOrders()
	{
		return ShowFinanceFeature("收款单", "用于维护正式收款单据、收款明细、核销关系和确认状态。");
	}

	public IActionResult OtherReceipts()
	{
		return ShowFinanceFeature("其他收款", "用于维护非销售类收款、杂项收入、押金收回和其他到账记录。");
	}

	public IActionResult Payments()
	{
		return ShowFinanceFeature("付款管理", "用于维护供应商付款、费用付款、付款核销和付款状态。");
	}

	public IActionResult AccountsPayable()
	{
		return ShowFinanceFeature("应付账", "用于查询供应商应付余额、账龄、到期款项和应付核销状态。");
	}

	public IActionResult CreditNotes()
	{
		return ShowFinanceFeature("贷记单", "用于维护供应商贷记单、扣减原因、应付调整和关联单据。");
	}

	public IActionResult AdvancePayments()
	{
		return ShowFinanceFeature("预付款", "用于维护供应商预付款登记、余额、占用和后续核销记录。");
	}

	public IActionResult PaymentOrders()
	{
		return ShowFinanceFeature("付款单", "用于维护正式付款单据、付款明细、核销关系和确认状态。");
	}

	public IActionResult OtherPayments()
	{
		return ShowFinanceFeature("其他付款", "用于维护非采购类付款、杂项支出、押金支付和其他付款记录。");
	}

	public IActionResult ExpenseReimbursements()
	{
		return ShowFinanceFeature("费用报销", "用于维护员工费用报销、报销明细、审批状态和付款记录。");
	}

	public IActionResult PayrollRecords()
	{
		return ShowFinanceFeature("工资记录", "用于维护工资发放记录、薪资明细、扣款项目和发放状态。");
	}

	public IActionResult AccountingVouchers()
	{
		return ShowFinanceFeature("记账凭证", "用于维护会计凭证、借贷分录、附件资料和过账状态。");
	}

	public IActionResult BatchPosting()
	{
		return ShowFinanceFeature("批量记账", "用于批量生成或提交记账凭证、处理结果和异常记录。");
	}

	public IActionResult VoucherReviews()
	{
		return ShowFinanceFeature("凭证审核", "用于审核记账凭证、记录审核意见、退回原因和审核状态。");
	}

	public IActionResult CheckCashings()
	{
		return ShowFinanceFeature("支票兑现", "用于维护支票到账、兑现日期、银行信息和兑现状态。");
	}

	public IActionResult BankReconciliations()
	{
		return ShowFinanceFeature("银行对账", "用于维护银行流水匹配、对账差异、调节项目和确认状态。");
	}

	public IActionResult CostAdjustments()
	{
		return ShowFinanceFeature("成本调整", "用于维护库存成本、采购成本、生产成本的调整单据和影响结果。");
	}

	public IActionResult AccruedReceipts()
	{
		return ShowFinanceFeature("暂估入库", "用于维护未到票入库的暂估金额、冲回状态和成本确认信息。");
	}

	public IActionResult CostTransfers()
	{
		return ShowFinanceFeature("成本转移", "用于维护成本在组织、仓库、项目或产品之间的转移记录。");
	}

	public IActionResult InventoryMonthlyClosing()
	{
		return ShowFinanceFeature("库存月结", "用于执行库存期间结算、成本汇总、差异检查和月结状态确认。");
	}

	public IActionResult MonthlyPosting()
	{
		return ShowFinanceFeature("月结过账", "用于处理月末凭证生成、期间损益结转和财务期间锁定。");
	}

	public IActionResult YearlyPosting()
	{
		return ShowFinanceFeature("年结过账", "用于处理年度结账、损益结转、余额结转和年度关闭。");
	}

	public IActionResult YearOpeningBalances()
	{
		return ShowFinanceFeature("年初始账", "用于维护新年度科目期初余额、辅助核算余额和启用状态。");
	}

	public IActionResult Statistics()
	{
		return ShowFinanceFeature("财务统计", "用于汇总财务收入、支出、成本、利润和资金状况统计。");
	}

	public IActionResult AccountBalanceReports()
	{
		return ShowFinanceFeature("科目余额表", "用于按期间、科目和组织查询期初、本期发生和期末余额。");
	}

	public IActionResult DetailLedgers()
	{
		return ShowFinanceFeature("明细账", "用于查询科目明细分录、凭证来源、借贷发生额和余额变化。");
	}

	public IActionResult IncomeStatements()
	{
		return ShowFinanceFeature("利润表", "用于统计收入、成本、费用、利润和期间损益情况。");
	}

	public IActionResult CashFlowStatements()
	{
		return ShowFinanceFeature("现金流量表", "用于统计经营、投资、筹资活动现金流入流出和净额。");
	}

	public IActionResult BalanceSheets()
	{
		return ShowFinanceFeature("资产负债表", "用于统计资产、负债、所有者权益和期末财务状况。");
	}

	private IActionResult ShowFinanceFeature(string featureName, string featureDescription)
	{
		base.ViewData["Title"] = featureName;
		base.ViewData["FeatureName"] = featureName;
		base.ViewData["FeatureDescription"] = featureDescription;
		return View("Index");
	}
}
