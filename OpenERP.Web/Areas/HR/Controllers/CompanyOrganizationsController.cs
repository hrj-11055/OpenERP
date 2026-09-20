using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using OpenERP.BasicData.Data;
using OpenERP.BasicData.Models;
using OpenERP.HR.Models.Entities;
using OpenERP.Web.Areas.HR.ViewModels.CompanyOrganizations;
using OpenERP.Web.Data.HR;
using OpenERP.Web.Localization;
using OpenERP.Web.Printing;

namespace OpenERP.Web.Areas.HR.Controllers;

[Area("HR")]
public class CompanyOrganizationsController : Controller
{
	private readonly IHrRepository _hrRepository;

	private readonly IBasicDataRepository _basicDataRepository;

	private readonly IStringLocalizer<SharedResource> _localizer;

	private readonly ICompanyPrintTemplateService _companyPrintTemplateService;

	private const string CountryRegionTypeCode = "COUNTRY_REGION";

	private const string CityTypeCode = "CITY";

	private const string CountyTypeCode = "COUNTY";

	public CompanyOrganizationsController(IHrRepository hrRepository, IBasicDataRepository basicDataRepository, IStringLocalizer<SharedResource> localizer, ICompanyPrintTemplateService companyPrintTemplateService)
	{
		_hrRepository = hrRepository;
		_basicDataRepository = basicDataRepository;
		_localizer = localizer;
		_companyPrintTemplateService = companyPrintTemplateService;
	}

	public async Task<IActionResult> Index(string? keyword, int? statusId)
	{
		List<CompanyOrganization> organizations = await _hrRepository.GetCompanyOrganizationsAsync();
		List<Employee> employees = await _hrRepository.GetEmployeesAsync();
		List<BasicDataItem> activeItems = (await _basicDataRepository.GetItemsAsync()).Where((BasicDataItem item) => !item.IsDeleted && item.IsActive).ToList();
		Dictionary<int, string> itemNameMap = activeItems.ToDictionary((BasicDataItem item) => item.Id, (BasicDataItem item) => item.ItemName);
		BasicDataType statusType = await _basicDataRepository.GetTypeByCodeAsync("ORG_STATUS");
		string normalizedKeyword = keyword?.Trim();
		string allText = _localizer["Common_All"].Value;
		string normalText = _localizer["Common_Normal"].Value;
		string notConfiguredText = _localizer["Common_NotConfigured"].Value;
		string notAssignedText = _localizer["Common_NotAssigned"].Value;
		string adminText = _localizer["Common_Admin"].Value;
		IReadOnlyList<SelectListItem> statusOptions = BuildStatusOptions(activeItems, statusType?.Id, statusId, allText);
		List<CompanyOrganization> filteredOrganizations = (from organization in organizations
			where !statusId.HasValue || organization.StatusId == statusId
			where string.IsNullOrWhiteSpace(normalizedKeyword) || MatchesKeyword(organization, normalizedKeyword)
			select organization).ToList();
		List<CompanyOrganizationListItemViewModel> records = filteredOrganizations.Select((CompanyOrganization organization, int index) => BuildListItem(organization, index + 1, itemNameMap, normalText, notConfiguredText, notAssignedText, adminText)).ToList();
		CompanyOrganizationIndexViewModel viewModel = new CompanyOrganizationIndexViewModel
		{
			Keyword = (normalizedKeyword ?? string.Empty),
			StatusId = statusId,
			StatusOptions = statusOptions,
			Records = records,
			EmployeeCards = BuildEmployeeCards(filteredOrganizations, employees),
			DefaultOrganizationId = records.FirstOrDefault()?.Id,
			FilteredCount = records.Count,
			TotalCount = organizations.Count
		};
		return View(viewModel);
	}

	public async Task<IActionResult> Create(bool popup = false, int? copyFromId = null)
	{
		CompanyOrganization organization;
		if (copyFromId.HasValue && copyFromId.Value > 0)
		{
			CompanyOrganization source = await _hrRepository.GetCompanyOrganizationByIdAsync(copyFromId.Value);
			organization = ((source == null) ? new CompanyOrganization() : CloneCompanyOrganizationForCopy(source));
		}
		else
		{
			organization = new CompanyOrganization();
		}
		base.ViewBag.IsPopup = popup;
		await PopulateSelectListsAsync(organization);
		return View(organization);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create([Bind(new string[] { "OrganizationCode,OrganizationName,CompanyNatureId,StatusId,EnterpriseTypeId,BusinessRegistrationNumber,BusinessRegistrationExpiryDate,RegionId,CityId,CountyId,Address,Principal,Phone,Fax,Email,Website,WeeklyWorkDays,LeaveCountBasisType,HolidayType,AnnualLeaveCalculationMonthDay,AnnualLeaveGrantRule,AnnualLeaveGrantMonthDay,IsAnnualLeaveClearEnabled,AnnualLeaveClearMonthDay,IsCarryForwardAnnualLeaveAllowed,BaseAnnualLeaveDays,AnnualLeaveIncrementStartYears,AnnualLeaveIncrementPerYearDays,AnnualLeaveCapDays,AnnualLeaveMaxAccumulatedDays,PaidSickLeaveDaysPerYear,PaidSickLeaveSalaryRatio,PaidSickLeaveCalculationMonthDay,IsPaidSickLeaveClearEnabled,PaidSickLeaveClearMonthDay,EmployeeMpfMinimumSalary,Remarks,ArchivePath,PrintHeaderContent,PrintFooterContent" })] CompanyOrganization organization, bool popup = false)
	{
		await ValidateLocationSelectionAsync(organization.RegionId, organization.CityId, organization.CountyId, "RegionId", "CityId", "CountyId");
		if (base.ModelState.IsValid)
		{
			organization.CreatedBy = await ResolveCurrentOperatorDisplayNameAsync();
			await _hrRepository.CreateCompanyOrganizationAsync(organization);
			IActionResult result;
			if (!popup)
			{
				IActionResult actionResult = RedirectToAction("Index");
				result = actionResult;
			}
			else
			{
				IActionResult actionResult = BuildDetailPageCloseResult();
				result = actionResult;
			}
			return result;
		}
		base.ViewBag.IsPopup = popup;
		await PopulateSelectListsAsync(organization, applyDefaults: true);
		return View(organization);
	}

	public async Task<IActionResult> Edit(int? id, bool popup = false)
	{
		if (!id.HasValue)
		{
			return NotFound();
		}
		CompanyOrganization organization = await _hrRepository.GetCompanyOrganizationByIdAsync(id.Value);
		if (organization == null)
		{
			return NotFound();
		}
		base.ViewBag.IsPopup = popup;
		await PopulateSelectListsAsync(organization);
		return View(organization);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(int id, [Bind(new string[] { "Id,OrganizationCode,OrganizationName,CompanyNatureId,StatusId,EnterpriseTypeId,BusinessRegistrationNumber,BusinessRegistrationExpiryDate,RegionId,CityId,CountyId,Address,Principal,Phone,Fax,Email,Website,WeeklyWorkDays,LeaveCountBasisType,HolidayType,AnnualLeaveCalculationMonthDay,AnnualLeaveGrantRule,AnnualLeaveGrantMonthDay,IsAnnualLeaveClearEnabled,AnnualLeaveClearMonthDay,IsCarryForwardAnnualLeaveAllowed,BaseAnnualLeaveDays,AnnualLeaveIncrementStartYears,AnnualLeaveIncrementPerYearDays,AnnualLeaveCapDays,AnnualLeaveMaxAccumulatedDays,PaidSickLeaveDaysPerYear,PaidSickLeaveSalaryRatio,PaidSickLeaveCalculationMonthDay,IsPaidSickLeaveClearEnabled,PaidSickLeaveClearMonthDay,EmployeeMpfMinimumSalary,Remarks,ArchivePath,PrintHeaderContent,PrintFooterContent" })] CompanyOrganization organization, bool popup = false)
	{
		if (id != organization.Id)
		{
			return NotFound();
		}
		await ValidateLocationSelectionAsync(organization.RegionId, organization.CityId, organization.CountyId, "RegionId", "CityId", "CountyId");
		if (base.ModelState.IsValid)
		{
			CompanyOrganization originalOrganization = await _hrRepository.GetCompanyOrganizationByIdAsync(id);
			organization.UpdatedBy = await ResolveCurrentOperatorDisplayNameAsync();
			if (!(await _hrRepository.UpdateCompanyOrganizationAsync(organization)))
			{
				return NotFound();
			}
			await _companyPrintTemplateService.RenameLogoAsync(originalOrganization?.OrganizationCode, organization.OrganizationCode);
			IActionResult result;
			if (!popup)
			{
				IActionResult actionResult = RedirectToAction("Index");
				result = actionResult;
			}
			else
			{
				IActionResult actionResult = BuildDetailPageCloseResult();
				result = actionResult;
			}
			return result;
		}
		base.ViewBag.IsPopup = popup;
		await PopulateSelectListsAsync(organization);
		return View(organization);
	}

	public async Task<IActionResult> Details(int? id, bool popup = false)
	{
		if (!id.HasValue)
		{
			return NotFound();
		}
		CompanyOrganization organization = await _hrRepository.GetCompanyOrganizationByIdAsync(id.Value);
		if (organization == null)
		{
			return NotFound();
		}
		base.ViewBag.IsPopup = popup;
		base.ViewBag.IsReadOnly = true;
		await PopulateSelectListsAsync(organization);
		return View(organization);
	}

	public async Task<IActionResult> Delete(int? id, bool popup = false)
	{
		if (!id.HasValue)
		{
			return NotFound();
		}
		CompanyOrganization organization = await _hrRepository.GetCompanyOrganizationByIdAsync(id.Value);
		if (organization == null)
		{
			return NotFound();
		}
		base.ViewBag.IsPopup = popup;
		return View(organization);
	}

	[HttpPost]
	[ActionName("Delete")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteConfirmed(int id, bool popup = false)
	{
		await _hrRepository.DeleteCompanyOrganizationAsync(id);
		IActionResult result;
		if (!popup)
		{
			IActionResult actionResult = RedirectToAction("Index");
			result = actionResult;
		}
		else
		{
			IActionResult actionResult = BuildDetailPageCloseResult();
			result = actionResult;
		}
		return result;
	}

	[HttpGet]
	public async Task<IActionResult> LocationOptions(string level, int? parentId = null)
	{
		string normalizedLevel = level?.Trim().ToUpperInvariant();
		if (1 == 0)
		{
		}
		string text = normalizedLevel switch
		{
			"COUNTRY_REGION" => "COUNTRY_REGION", 
			"CITY" => "CITY", 
			"COUNTY" => "COUNTY", 
			_ => string.Empty, 
		};
		if (1 == 0)
		{
		}
		string typeCode = text;
		if (string.IsNullOrWhiteSpace(typeCode))
		{
			return Json(Array.Empty<object>());
		}
		return Json((await GetLocationItemsAsync(typeCode, parentId)).Select((BasicDataItem item) => new
		{
			value = item.Id,
			text = item.ItemName
		}));
	}

	private async Task PopulateSelectListsAsync(CompanyOrganization? organization = null, bool applyDefaults = false)
	{
		if (organization == null)
		{
			organization = new CompanyOrganization();
		}
		List<BasicDataType> types = await _basicDataRepository.GetTypesAsync();
		List<BasicDataItem> activeItems = (await _basicDataRepository.GetItemsAsync()).Where((BasicDataItem item) => !item.IsDeleted && item.IsActive).ToList();
		Dictionary<string, int> typeIdMap = types.ToDictionary<BasicDataType, string, int>((BasicDataType type) => type.TypeCode, (BasicDataType type) => type.Id, StringComparer.OrdinalIgnoreCase);
		int? selectedCountryRegionId = organization.RegionId;
		int? selectedCityId = organization.CityId;
		int? selectedCountyId = organization.CountyId;
		NormalizeLocationSelection(activeItems, typeIdMap, ref selectedCountryRegionId, ref selectedCityId, ref selectedCountyId);
		if (applyDefaults)
		{
			CompanyOrganization companyOrganization = organization;
			if (!companyOrganization.CompanyNatureId.HasValue)
			{
				companyOrganization.CompanyNatureId = FindDefaultItemId(activeItems, typeIdMap, "COMPANY_NATURE", "PRIVATE");
			}
			companyOrganization = organization;
			if (!companyOrganization.StatusId.HasValue)
			{
				companyOrganization.StatusId = FindDefaultItemId(activeItems, typeIdMap, "ORG_STATUS", "ACTIVE");
			}
			companyOrganization = organization;
			if (!companyOrganization.EnterpriseTypeId.HasValue)
			{
				companyOrganization.EnterpriseTypeId = FindDefaultItemId(activeItems, typeIdMap, "ENTERPRISE_TYPE", "LLC");
			}
			int? num = selectedCountryRegionId;
			if (!num.HasValue)
			{
				selectedCountryRegionId = FindDefaultItemId(activeItems, typeIdMap, "COUNTRY_REGION", "CN");
			}
			num = selectedCityId;
			if (!num.HasValue)
			{
				selectedCityId = FindDefaultChildItemId(activeItems, typeIdMap, "CITY", selectedCountryRegionId, "GUANGZHOU");
			}
			num = selectedCountyId;
			if (!num.HasValue)
			{
				selectedCountyId = FindDefaultChildItemId(activeItems, typeIdMap, "COUNTY", selectedCityId, "TIANHE");
			}
			companyOrganization = organization;
			decimal? weeklyWorkDays = companyOrganization.WeeklyWorkDays;
			weeklyWorkDays.GetValueOrDefault();
			if (!weeklyWorkDays.HasValue)
			{
				decimal value = 5m;
				companyOrganization.WeeklyWorkDays = value;
			}
		}
		organization.RegionId = selectedCountryRegionId;
		organization.CityId = selectedCityId;
		organization.CountyId = selectedCountyId;
		base.ViewData["CompanyNatureId"] = BuildSelectList(activeItems, typeIdMap, "COMPANY_NATURE", organization.CompanyNatureId);
		base.ViewData["StatusId"] = BuildSelectList(activeItems, typeIdMap, "ORG_STATUS", organization.StatusId);
		base.ViewData["EnterpriseTypeId"] = BuildSelectList(activeItems, typeIdMap, "ENTERPRISE_TYPE", organization.EnterpriseTypeId);
		base.ViewData["RegionId"] = BuildSelectList(activeItems, typeIdMap, "COUNTRY_REGION", organization.RegionId);
		base.ViewData["CityId"] = BuildSelectList(activeItems, typeIdMap, "CITY", organization.CityId, organization.RegionId);
		base.ViewData["CountyId"] = BuildSelectList(activeItems, typeIdMap, "COUNTY", organization.CountyId, organization.CityId);
	}

	private static List<SelectListItem> BuildSelectList(IEnumerable<BasicDataItem> activeItems, IReadOnlyDictionary<string, int> typeIdMap, string typeCode, int? selectedValue, int? parentId = null)
	{
		if (!typeIdMap.TryGetValue(typeCode, out var typeId))
		{
			return new List<SelectListItem>();
		}
		return (from item in activeItems
			where item.TypeId == typeId
			where !parentId.HasValue || item.ParentId == parentId.Value
			orderby item.SortOrder, item.ItemName
			select new SelectListItem(item.ItemName, item.Id.ToString(), item.Id == selectedValue)).ToList();
	}

	private static int? FindDefaultItemId(IEnumerable<BasicDataItem> activeItems, IReadOnlyDictionary<string, int> typeIdMap, string typeCode, string itemCode)
	{
		if (!typeIdMap.TryGetValue(typeCode, out var typeId))
		{
			return null;
		}
		return activeItems.FirstOrDefault((BasicDataItem item) => item.TypeId == typeId && string.Equals(item.ItemCode, itemCode, StringComparison.OrdinalIgnoreCase))?.Id;
	}

	private static int? FindDefaultChildItemId(IEnumerable<BasicDataItem> activeItems, IReadOnlyDictionary<string, int> typeIdMap, string typeCode, int? parentId, string itemCode)
	{
		if (!parentId.HasValue || !typeIdMap.TryGetValue(typeCode, out var typeId))
		{
			return null;
		}
		return activeItems.FirstOrDefault((BasicDataItem item) => item.TypeId == typeId && item.ParentId == parentId.Value && string.Equals(item.ItemCode, itemCode, StringComparison.OrdinalIgnoreCase))?.Id;
	}

	private async Task<List<BasicDataItem>> GetLocationItemsAsync(string typeCode, int? parentId = null)
	{
		List<BasicDataType> types = await _basicDataRepository.GetTypesAsync();
		List<BasicDataItem> activeItems = (await _basicDataRepository.GetItemsAsync()).Where((BasicDataItem item) => !item.IsDeleted && item.IsActive).ToList();
		Dictionary<string, int> typeIdMap = types.ToDictionary<BasicDataType, string, int>((BasicDataType type) => type.TypeCode, (BasicDataType type) => type.Id, StringComparer.OrdinalIgnoreCase);
		if (!typeIdMap.TryGetValue(typeCode, out var typeId))
		{
			return new List<BasicDataItem>();
		}
		return (from item in activeItems
			where item.TypeId == typeId
			where !parentId.HasValue || item.ParentId == parentId.Value
			orderby item.SortOrder, item.ItemName
			select item).ToList();
	}

	private async Task ValidateLocationSelectionAsync(int? countryRegionId, int? cityId, int? countyId, string countryRegionFieldName, string cityFieldName, string countyFieldName)
	{
		List<BasicDataType> types = await _basicDataRepository.GetTypesAsync();
		List<BasicDataItem> activeItems = (await _basicDataRepository.GetItemsAsync()).Where((BasicDataItem item) => !item.IsDeleted && item.IsActive).ToList();
		Dictionary<string, int> typeIdMap = types.ToDictionary<BasicDataType, string, int>((BasicDataType type) => type.TypeCode, (BasicDataType type) => type.Id, StringComparer.OrdinalIgnoreCase);
		BasicDataItem countryRegionItem = FindActiveItem(activeItems, typeIdMap, "COUNTRY_REGION", countryRegionId);
		BasicDataItem cityItem = FindActiveItem(activeItems, typeIdMap, "CITY", cityId);
		BasicDataItem countyItem = FindActiveItem(activeItems, typeIdMap, "COUNTY", countyId);
		if (countryRegionId.HasValue && countryRegionItem == null)
		{
			base.ModelState.AddModelError(countryRegionFieldName, "所选国家/地区不存在或已停用。");
		}
		if (cityId.HasValue && cityItem == null)
		{
			base.ModelState.AddModelError(cityFieldName, "所选城市不存在或已停用。");
		}
		if (countyId.HasValue && countyItem == null)
		{
			base.ModelState.AddModelError(countyFieldName, "所选县/区不存在或已停用。");
		}
		if (cityItem != null)
		{
			if (!countryRegionId.HasValue)
			{
				base.ModelState.AddModelError(countryRegionFieldName, "选择城市前请先选择国家/地区。");
			}
			else if (cityItem.ParentId != countryRegionId.Value)
			{
				base.ModelState.AddModelError(cityFieldName, "所选城市不属于当前国家/地区。");
			}
		}
		if (countyItem != null)
		{
			if (!cityId.HasValue)
			{
				base.ModelState.AddModelError(cityFieldName, "选择县/区前请先选择城市。");
			}
			else if (countyItem.ParentId != cityId.Value)
			{
				base.ModelState.AddModelError(countyFieldName, "所选县/区不属于当前城市。");
			}
		}
	}

	private static void NormalizeLocationSelection(IReadOnlyCollection<BasicDataItem> activeItems, IReadOnlyDictionary<string, int> typeIdMap, ref int? countryRegionId, ref int? cityId, ref int? countyId)
	{
		BasicDataItem basicDataItem = FindActiveItem(activeItems, typeIdMap, "COUNTRY_REGION", countryRegionId);
		BasicDataItem basicDataItem2 = FindActiveItem(activeItems, typeIdMap, "CITY", cityId);
		BasicDataItem basicDataItem3 = FindActiveItem(activeItems, typeIdMap, "COUNTY", countyId);
		if (countyId.HasValue && basicDataItem3 == null)
		{
			countyId = null;
		}
		if (cityId.HasValue && basicDataItem2 == null)
		{
			cityId = null;
		}
		if (countryRegionId.HasValue && basicDataItem == null)
		{
			countryRegionId = null;
		}
		basicDataItem3 = FindActiveItem(activeItems, typeIdMap, "COUNTY", countyId);
		if (basicDataItem3 != null && !cityId.HasValue)
		{
			cityId = basicDataItem3.ParentId;
		}
		basicDataItem2 = FindActiveItem(activeItems, typeIdMap, "CITY", cityId);
		if (basicDataItem2 != null && !countryRegionId.HasValue)
		{
			countryRegionId = basicDataItem2.ParentId;
		}
		basicDataItem2 = FindActiveItem(activeItems, typeIdMap, "CITY", cityId);
		if (basicDataItem2 != null && countryRegionId.HasValue && basicDataItem2.ParentId != countryRegionId.Value)
		{
			cityId = null;
			countyId = null;
		}
		basicDataItem3 = FindActiveItem(activeItems, typeIdMap, "COUNTY", countyId);
		if (basicDataItem3 != null && cityId.HasValue && basicDataItem3.ParentId != cityId.Value)
		{
			countyId = null;
		}
	}

	private static BasicDataItem? FindActiveItem(IEnumerable<BasicDataItem> activeItems, IReadOnlyDictionary<string, int> typeIdMap, string typeCode, int? itemId)
	{
		if (!itemId.HasValue || !typeIdMap.TryGetValue(typeCode, out var typeId))
		{
			return null;
		}
		return activeItems.FirstOrDefault((BasicDataItem item) => item.Id == itemId.Value && item.TypeId == typeId);
	}

	private static CompanyOrganization CloneCompanyOrganizationForCopy(CompanyOrganization source)
	{
		string text = source.OrganizationCode;
		if (!string.IsNullOrWhiteSpace(text))
		{
			text += "-C";
			if (text.Length > 10)
			{
				text = text.Substring(0, 10);
			}
		}
		return new CompanyOrganization
		{
			OrganizationCode = (text ?? string.Empty),
			OrganizationName = (string.IsNullOrWhiteSpace(source.OrganizationName) ? string.Empty : (source.OrganizationName + "（副本）")),
			CompanyNatureId = source.CompanyNatureId,
			StatusId = source.StatusId,
			EnterpriseTypeId = source.EnterpriseTypeId,
			BusinessRegistrationNumber = source.BusinessRegistrationNumber,
			BusinessRegistrationExpiryDate = source.BusinessRegistrationExpiryDate,
			RegionId = source.RegionId,
			CityId = source.CityId,
			CountyId = source.CountyId,
			Address = source.Address,
			Principal = source.Principal,
			Phone = source.Phone,
			Fax = source.Fax,
			Email = source.Email,
			Website = source.Website,
			WeeklyWorkDays = source.WeeklyWorkDays,
			LeaveCountBasisType = source.LeaveCountBasisType,
			HolidayType = source.HolidayType,
			AnnualLeaveCalculationMonthDay = source.AnnualLeaveCalculationMonthDay,
			AnnualLeaveGrantRule = source.AnnualLeaveGrantRule,
			AnnualLeaveGrantMonthDay = source.AnnualLeaveGrantMonthDay,
			IsAnnualLeaveClearEnabled = source.IsAnnualLeaveClearEnabled,
			AnnualLeaveClearMonthDay = source.AnnualLeaveClearMonthDay,
			IsCarryForwardAnnualLeaveAllowed = source.IsCarryForwardAnnualLeaveAllowed,
			BaseAnnualLeaveDays = source.BaseAnnualLeaveDays,
			AnnualLeaveIncrementStartYears = source.AnnualLeaveIncrementStartYears,
			AnnualLeaveIncrementPerYearDays = source.AnnualLeaveIncrementPerYearDays,
			AnnualLeaveCapDays = source.AnnualLeaveCapDays,
			AnnualLeaveMaxAccumulatedDays = source.AnnualLeaveMaxAccumulatedDays,
			PaidSickLeaveDaysPerYear = source.PaidSickLeaveDaysPerYear,
			PaidSickLeaveSalaryRatio = source.PaidSickLeaveSalaryRatio,
			PaidSickLeaveCalculationMonthDay = source.PaidSickLeaveCalculationMonthDay,
			IsPaidSickLeaveClearEnabled = source.IsPaidSickLeaveClearEnabled,
			PaidSickLeaveClearMonthDay = source.PaidSickLeaveClearMonthDay,
			EmployeeMpfMinimumSalary = source.EmployeeMpfMinimumSalary,
			Remarks = source.Remarks,
			ArchivePath = source.ArchivePath,
			PrintHeaderContent = source.PrintHeaderContent,
			PrintFooterContent = source.PrintFooterContent
		};
	}

	private ContentResult BuildDetailPageCloseResult()
	{
		LocalizedString value = _localizer["Common_Processing"];
		string content = $"<!DOCTYPE html>\n<html lang=\"zh-CN\">\n<head>\n    <meta charset=\"utf-8\" />\n    <title>{value}</title>\n</head>\n<body>\n    <script>\n        const closeMessage = {{\n            type: \"open-erp:employee-modal-close\",\n            refreshRequested: true\n        }};\n\n        if (window.parent && window.parent !== window) {{\n            window.parent.postMessage(closeMessage, window.location.origin);\n        }} else if (window.opener && !window.opener.closed) {{\n            window.opener.postMessage(closeMessage, window.location.origin);\n            window.close();\n        }}\n    </script>\n</body>\n</html>";
		return Content(content, "text/html; charset=utf-8");
	}

	private static IReadOnlyList<SelectListItem> BuildStatusOptions(IEnumerable<BasicDataItem> activeItems, int? statusTypeId, int? selectedStatusId, string allText)
	{
		if (!statusTypeId.HasValue)
		{
			return new List<SelectListItem>
			{
				new SelectListItem(allText, string.Empty, !selectedStatusId.HasValue)
			};
		}
		List<SelectListItem> list = (from item in activeItems
			where item.TypeId == statusTypeId.Value
			orderby item.SortOrder
			select new SelectListItem(item.ItemName, item.Id.ToString(), item.Id == selectedStatusId)).ToList();
		list.Insert(0, new SelectListItem(allText, string.Empty, !selectedStatusId.HasValue));
		return list;
	}

	private static CompanyOrganizationListItemViewModel BuildListItem(CompanyOrganization organization, int sequenceNo, IReadOnlyDictionary<int, string> itemNameMap, string normalText, string notConfiguredText, string notAssignedText, string adminText)
	{
		DateTime lastModifiedAt = organization.UpdatedAt ?? organization.CreatedAt;
		string lastModifiedBy = organization.UpdatedBy ?? organization.CreatedBy ?? adminText;
		return new CompanyOrganizationListItemViewModel
		{
			Id = organization.Id,
			SequenceNo = sequenceNo,
			OrganizationCode = organization.OrganizationCode,
			OrganizationShortName = BuildOrganizationShortName(organization.OrganizationName),
			CompanyName = organization.OrganizationName,
			StatusName = ResolveItemName(itemNameMap, organization.StatusId, normalText),
			EnterpriseTypeName = ResolveItemName(itemNameMap, organization.EnterpriseTypeId, notConfiguredText),
			RegionDisplayName = BuildRegionDisplayName(organization, itemNameMap, notConfiguredText),
			Principal = (organization.Principal ?? notAssignedText),
			Phone = (organization.Phone ?? "-"),
			Fax = (organization.Fax ?? "-"),
			Email = (organization.Email ?? "-"),
			Remarks = (organization.Remarks ?? "-"),
			ArchivePath = (organization.ArchivePath ?? string.Empty),
			LastModifiedBy = lastModifiedBy,
			LastModifiedAt = lastModifiedAt,
			LastModifiedAtText = lastModifiedAt.ToString("yyyy-MM-dd HH:mm")
		};
	}

	private IReadOnlyList<CompanyOrganizationEmployeeCardViewModel> BuildEmployeeCards(IEnumerable<CompanyOrganization> organizations, IEnumerable<Employee> employees)
	{
		string activeText = _localizer["Employee_Status_Active"].Value;
		string leftText = _localizer["Employee_Status_Left"].Value;
		string unnamedEmployeeText = _localizer["Employee_NotNamed"].Value;
		string unsetJobTitleText = _localizer["CompanyOrganization_UnsetJobTitle"].Value;
		string unassignedDepartmentText = _localizer["CompanyOrganization_UnassignedDepartment"].Value;
		Dictionary<int, List<Employee>> employeeGroups = (from employee in employees
			where !employee.IsDeleted && employee.OrganizationId.HasValue
			group employee by employee.OrganizationId.Value).ToDictionary((IGrouping<int, Employee> group) => group.Key, (IGrouping<int, Employee> group) => group.ToList());
		return organizations.Select(delegate(CompanyOrganization organization)
		{
			employeeGroups.TryGetValue(organization.Id, out var value);
			return BuildEmployeeCard(organization, value ?? new List<Employee>(), activeText, leftText, unnamedEmployeeText, unsetJobTitleText, unassignedDepartmentText);
		}).ToList();
	}

	private static CompanyOrganizationEmployeeCardViewModel BuildEmployeeCard(CompanyOrganization organization, IReadOnlyList<Employee> employees, string activeText, string leftText, string unnamedEmployeeText, string unsetJobTitleText, string unassignedDepartmentText)
	{
		List<CompanyOrganizationEmployeeListItemViewModel> list = (from employee in employees
			orderby employee.Department?.Name ?? string.Empty, employee.LastName, employee.FirstName
			select employee).Select((Employee employee, int index) => BuildEmployeeCardItem(employee, index + 1, activeText, leftText, unnamedEmployeeText, unsetJobTitleText, unassignedDepartmentText)).ToList();
		return new CompanyOrganizationEmployeeCardViewModel
		{
			OrganizationId = organization.Id,
			OrganizationCode = organization.OrganizationCode,
			OrganizationName = organization.OrganizationName,
			EmployeeCount = list.Count,
			ActiveEmployeeCount = employees.Count((Employee employee) => !employee.LeaveDate.HasValue),
			DepartmentCount = (from employee in employees
				select employee.Department?.Name into name
				where !string.IsNullOrWhiteSpace(name)
				select name).Distinct<string>(StringComparer.OrdinalIgnoreCase).Count(),
			Employees = list
		};
	}

	private static CompanyOrganizationEmployeeListItemViewModel BuildEmployeeCardItem(Employee employee, int sequenceNo, string activeText, string leftText, string unnamedEmployeeText, string unsetJobTitleText, string unassignedDepartmentText)
	{
		return new CompanyOrganizationEmployeeListItemViewModel
		{
			EmployeeId = employee.Id,
			EmployeeCode = ((!string.IsNullOrWhiteSpace(employee.EmployeeCode)) ? employee.EmployeeCode : $"A{sequenceNo:000}"),
			DisplayName = BuildEmployeeDisplayName(employee, unnamedEmployeeText),
			JobTitle = ((!string.IsNullOrWhiteSpace(employee.JobTitle)) ? employee.JobTitle : (employee.Position?.Name ?? unsetJobTitleText)),
			DepartmentName = (employee.Department?.Name ?? unassignedDepartmentText),
			EmploymentStatusName = (employee.LeaveDate.HasValue ? leftText : activeText),
			Phone = (employee.PhoneNumber ?? "-"),
			Email = employee.Email
		};
	}

	private static string BuildEmployeeDisplayName(Employee employee, string unnamedEmployeeText)
	{
		string text = employee.FirstName?.Trim() ?? string.Empty;
		string text2 = employee.LastName?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(text2))
		{
			return unnamedEmployeeText;
		}
		return (ContainsLatinLetter(text) || ContainsLatinLetter(text2)) ? string.Join(" ", new string[2] { text, text2 }.Where((string value) => !string.IsNullOrWhiteSpace(value))) : (text2 + text);
	}

	private static bool ContainsLatinLetter(string value)
	{
		return value.Any(delegate(char character)
		{
			switch (character)
			{
			case 'A':
			case 'B':
			case 'C':
			case 'D':
			case 'E':
			case 'F':
			case 'G':
			case 'H':
			case 'I':
			case 'J':
			case 'K':
			case 'L':
			case 'M':
			case 'N':
			case 'O':
			case 'P':
			case 'Q':
			case 'R':
			case 'S':
			case 'T':
			case 'U':
			case 'V':
			case 'W':
			case 'X':
			case 'Y':
			case 'Z':
			case 'a':
			case 'b':
			case 'c':
			case 'd':
			case 'e':
			case 'f':
			case 'g':
			case 'h':
			case 'i':
			case 'j':
			case 'k':
			case 'l':
			case 'm':
			case 'n':
			case 'o':
			case 'p':
			case 'q':
			case 'r':
			case 's':
			case 't':
			case 'u':
			case 'v':
			case 'w':
			case 'x':
			case 'y':
			case 'z':
				return true;
			default:
				return false;
			}
		});
	}

	private async Task<string> ResolveCurrentOperatorDisplayNameAsync()
	{
		string loginAccount = base.User.FindFirstValue("erp:account");
		if (!string.IsNullOrWhiteSpace(loginAccount))
		{
			string unnamedEmployeeText = _localizer["Employee_NotNamed"].Value;
			Employee currentEmployee = (await _hrRepository.GetEmployeesAsync()).FirstOrDefault((Employee employee) => string.Equals(employee.LoginAccount, loginAccount, StringComparison.OrdinalIgnoreCase));
			if (currentEmployee != null)
			{
				return BuildEmployeeDisplayName(currentEmployee, unnamedEmployeeText);
			}
		}
		return (!string.IsNullOrWhiteSpace(base.User.Identity?.Name)) ? base.User.Identity.Name : ((string?)_localizer["Common_Admin"]);
	}

	private static bool MatchesKeyword(CompanyOrganization organization, string normalizedKeyword)
	{
		return ContainsValue(organization.OrganizationCode, normalizedKeyword) || ContainsValue(organization.OrganizationName, normalizedKeyword) || ContainsValue(organization.Principal, normalizedKeyword) || ContainsValue(organization.Phone, normalizedKeyword) || ContainsValue(organization.Email, normalizedKeyword) || ContainsValue(organization.Remarks, normalizedKeyword);
	}

	private static bool ContainsValue(string? sourceValue, string keyword)
	{
		return !string.IsNullOrWhiteSpace(sourceValue) && sourceValue.Contains(keyword, StringComparison.OrdinalIgnoreCase);
	}

	private static string ResolveItemName(IReadOnlyDictionary<int, string> itemNameMap, int? itemId, string fallbackText)
	{
		if (!itemId.HasValue)
		{
			return fallbackText;
		}
		string value;
		return itemNameMap.TryGetValue(itemId.Value, out value) ? value : fallbackText;
	}

	private static string BuildRegionDisplayName(CompanyOrganization organization, IReadOnlyDictionary<int, string> itemNameMap, string notConfiguredText)
	{
		List<string> list = new string[3]
		{
			ResolveItemName(itemNameMap, organization.RegionId, string.Empty),
			ResolveItemName(itemNameMap, organization.CityId, string.Empty),
			ResolveItemName(itemNameMap, organization.CountyId, string.Empty)
		}.Where((string segment) => !string.IsNullOrWhiteSpace(segment)).ToList();
		return (list.Count == 0) ? notConfiguredText : string.Join(" / ", list);
	}

	private static string BuildOrganizationShortName(string organizationName)
	{
		string[] array = new string[4] { "股份有限公司", "集团有限公司", "有限责任公司", "有限公司" };
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (organizationName.EndsWith(text, StringComparison.Ordinal))
			{
				int length = text.Length;
				return organizationName.Substring(0, organizationName.Length - length);
			}
		}
		return organizationName;
	}
}
