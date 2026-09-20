using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenERP.Logistics.Data;
using OpenERP.Logistics.Models.Entities;
using OpenERP.Web.Areas.Logistics.ViewModels.MaterialItems;

namespace OpenERP.Web.Areas.Logistics.Controllers;

[Authorize]
[Area("Logistics")]
public class MaterialManagementController : Controller
{
	private readonly ApplicationDbContext _context;

	public MaterialManagementController(ApplicationDbContext context)
	{
		_context = context;
	}

	public Task<IActionResult> Products(string? scope, string? keyword, int? selectedItemId)
	{
		return ShowMaterialCategoryAsync("产品", "产品资料", scope, keyword, selectedItemId);
	}

	public Task<IActionResult> Materials(string? scope, string? keyword, int? selectedItemId)
	{
		return ShowMaterialCategoryAsync("材料", "材料资料", scope, keyword, selectedItemId);
	}

	public Task<IActionResult> AuxiliaryMaterials(string? scope, string? keyword, int? selectedItemId)
	{
		return ShowMaterialCategoryAsync("辅料", "辅料资料", scope, keyword, selectedItemId);
	}

	public IActionResult Create(string category = "产品", bool popup = false)
	{
		base.ViewBag.IsPopup = popup;
		return View("Edit", new MaterialItemEditViewModel
		{
			Category = NormalizeCategory(category)
		});
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(MaterialItemEditViewModel input, bool popup = false)
	{
		input.Category = NormalizeCategory(input.Category);
		await ValidateMaterialItemAsync(input, null);
		if (!base.ModelState.IsValid)
		{
			base.ViewBag.IsPopup = popup;
			return View("Edit", NormalizeDetailRows(input));
		}
		MaterialItem item = new MaterialItem
		{
			CreatedBy = GetCurrentUserName(),
			CreatedAt = DateTime.Now
		};
		ApplyItemValues(item, input);
		ApplyBomRows(item, input, new List<MaterialItemBomLine>());
		ApplyPriceRows(item, input, new List<MaterialItemPriceLine>());
		_context.MaterialItems.Add(item);
		await _context.SaveChangesAsync();
		IActionResult result;
		if (!popup)
		{
			IActionResult actionResult = RedirectToCategoryList(input.Category, item.Id);
			result = actionResult;
		}
		else
		{
			IActionResult actionResult = BuildDetailPageCloseResult();
			result = actionResult;
		}
		return result;
	}

	public async Task<IActionResult> Edit(int id, bool popup = false)
	{
		MaterialItem item = await LoadDetailItemAsync(id);
		if (item == null)
		{
			return NotFound();
		}
		base.ViewBag.IsPopup = popup;
		return View(ToEditModel(item));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(int id, MaterialItemEditViewModel input, bool popup = false)
	{
		if (input.Id != id)
		{
			return NotFound();
		}
		MaterialItem item = await LoadDetailItemAsync(id);
		if (item == null)
		{
			return NotFound();
		}
		input.Category = NormalizeCategory(input.Category);
		await ValidateMaterialItemAsync(input, id);
		if (!base.ModelState.IsValid)
		{
			base.ViewBag.IsPopup = popup;
			return View(NormalizeDetailRows(input));
		}
		ApplyItemValues(item, input);
		item.UpdatedBy = GetCurrentUserName();
		item.UpdatedAt = DateTime.Now;
		ApplyBomRows(item, input, item.BomLines.ToList());
		ApplyPriceRows(item, input, item.PriceLines.ToList());
		await _context.SaveChangesAsync();
		IActionResult result;
		if (!popup)
		{
			IActionResult actionResult = RedirectToCategoryList(input.Category, item.Id);
			result = actionResult;
		}
		else
		{
			IActionResult actionResult = BuildDetailPageCloseResult();
			result = actionResult;
		}
		return result;
	}

	public async Task<IActionResult> Details(int id, bool popup = false)
	{
		MaterialItem item = await LoadDetailItemAsync(id, asNoTracking: true);
		if (item == null)
		{
			return NotFound();
		}
		base.ViewBag.IsPopup = popup;
		base.ViewBag.IsReadOnly = true;
		return View("Edit", ToEditModel(item));
	}

	public async Task<IActionResult> Copy(int id, bool popup = false)
	{
		MaterialItem item = await LoadDetailItemAsync(id, asNoTracking: true);
		if (item == null)
		{
			return NotFound();
		}
		MaterialItemEditViewModel model = ToEditModel(item);
		model.Id = null;
		model.ItemCode += "_COPY";
		model.ItemName += " 副本";
		foreach (MaterialItemBomLineInputModel line in model.BomLines)
		{
			line.Id = null;
		}
		foreach (MaterialItemPriceLineInputModel line2 in model.PriceLines)
		{
			line2.Id = null;
		}
		base.ViewBag.IsPopup = popup;
		return View("Edit", model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Delete(string category, int[] ids)
	{
		int[] itemIds = ids.Where((int id) => id > 0).Distinct().ToArray();
		if (itemIds.Length == 0)
		{
			return BadRequest("请先选择要删除的产品资料。");
		}
		List<MaterialItem> items = await (from materialItem in _context.MaterialItems.Include((MaterialItem materialItem) => materialItem.BomLines).Include((MaterialItem materialItem) => materialItem.PriceLines)
			where itemIds.Contains(materialItem.Id)
			select materialItem).ToListAsync();
		if (items.Count == 0)
		{
			return NotFound();
		}
		string currentUserName = GetCurrentUserName();
		DateTime deletedAt = DateTime.Now;
		foreach (MaterialItem item in items)
		{
			item.IsDeleted = true;
			item.UpdatedBy = currentUserName;
			item.UpdatedAt = deletedAt;
			foreach (MaterialItemBomLine line in item.BomLines)
			{
				line.IsDeleted = true;
				line.UpdatedBy = currentUserName;
				line.UpdatedAt = deletedAt;
			}
			foreach (MaterialItemPriceLine line2 in item.PriceLines)
			{
				line2.IsDeleted = true;
				line2.UpdatedBy = currentUserName;
				line2.UpdatedAt = deletedAt;
			}
		}
		await _context.SaveChangesAsync();
		return RedirectToCategoryList(NormalizeCategory(category));
	}

	public async Task<IActionResult> Export(string category = "产品", string? scope = null, string? keyword = null)
	{
		string normalizedCategory = NormalizeCategory(category);
		List<MaterialItem> items = await (from materialItem in ApplyMaterialSearch(scope: string.IsNullOrWhiteSpace(scope) ? "全部" : scope.Trim(), keyword: keyword?.Trim(), query: from materialItem in _context.MaterialItems.AsNoTracking()
				where materialItem.ItemCategory == normalizedCategory
				select materialItem)
			orderby materialItem.ItemCode
			select materialItem).ToListAsync();
		StringBuilder csvBuilder = new StringBuilder();
		csvBuilder.AppendLine("产品编号,产品名称,状态,类型,品牌,规格/型号,产地,单位,库存,备注,最后修改人,最后修改时间");
		foreach (MaterialItem item in items)
		{
			string[] row = new string[12]
			{
				item.ItemCode,
				item.ItemName,
				item.Status,
				item.ItemType,
				item.Brand,
				BuildSpecificationModel(item),
				item.Origin,
				item.BaseUnit,
				FormatQuantity(item.InventoryQuantity),
				item.Remarks,
				item.UpdatedBy ?? item.CreatedBy,
				(item.UpdatedAt ?? item.CreatedAt).ToString("yyyy-MM-dd HH:mm")
			};
			csvBuilder.AppendLine(string.Join(",", row.Select(EscapeCsv)));
		}
		byte[] bytes = Encoding.UTF8.GetBytes("\ufeff" + csvBuilder);
		return File(bytes, "text/csv; charset=utf-8", $"{GetPageTitle(normalizedCategory)}_{DateTime.Now:yyyyMMddHHmmss}.csv");
	}

	private async Task<IActionResult> ShowMaterialCategoryAsync(string category, string pageTitle, string? scope, string? keyword, int? selectedItemId)
	{
		string queryScope = (string.IsNullOrWhiteSpace(scope) ? "全部" : scope.Trim());
		string queryKeyword = keyword?.Trim();
		List<MaterialItem> items = await (from item in ApplyMaterialSearch(from item in _context.MaterialItems.AsNoTracking()
				where item.ItemCategory == category
				select item, queryScope, queryKeyword)
			orderby item.ItemCode
			select item).ToListAsync();
		MaterialItem selectedItem = (selectedItemId.HasValue ? items.FirstOrDefault((MaterialItem item) => item.Id == selectedItemId.Value) : items.FirstOrDefault());
		MaterialItemIndexViewModel model = new MaterialItemIndexViewModel
		{
			Category = category,
			PageTitle = pageTitle,
			Scope = queryScope,
			Keyword = queryKeyword,
			SelectedItemId = selectedItem?.Id,
			SelectedItemName = ((selectedItem == null) ? ("未选择" + TrimCategorySuffix(pageTitle)) : (selectedItem.ItemCode + " " + selectedItem.ItemName)),
			Items = items.Select((MaterialItem item, int index) => ToListItem(item, index + 1)).ToList(),
			RelatedRecords = BuildRelatedRecords(selectedItem)
		};
		return View("Index", model);
	}

	private static IQueryable<MaterialItem> ApplyMaterialSearch(IQueryable<MaterialItem> query, string scope, string? keyword)
	{
		if (string.IsNullOrWhiteSpace(keyword))
		{
			return query;
		}
		if (1 == 0)
		{
		}
		IQueryable<MaterialItem> result;
		switch (scope)
		{
		case "产品编号":
		case "资料编号":
			result = query.Where((MaterialItem item) => item.ItemCode.Contains(keyword));
			break;
		case "产品名称":
		case "资料名称":
			result = query.Where((MaterialItem item) => item.ItemName.Contains(keyword));
			break;
		case "品牌":
			result = query.Where((MaterialItem item) => item.Brand != null && item.Brand.Contains(keyword));
			break;
		case "类型":
			result = query.Where((MaterialItem item) => item.ItemType != null && item.ItemType.Contains(keyword));
			break;
		case "备注":
			result = query.Where((MaterialItem item) => item.Remarks != null && item.Remarks.Contains(keyword));
			break;
		default:
			result = query.Where((MaterialItem item) => item.ItemCode.Contains(keyword) || item.ItemName.Contains(keyword) || item.Status.Contains(keyword) || (item.ItemType != null && item.ItemType.Contains(keyword)) || (item.Brand != null && item.Brand.Contains(keyword)) || (item.Model != null && item.Model.Contains(keyword)) || (item.Specification != null && item.Specification.Contains(keyword)) || (item.Origin != null && item.Origin.Contains(keyword)) || (item.Remarks != null && item.Remarks.Contains(keyword)));
			break;
		}
		if (1 == 0)
		{
		}
		return result;
	}

	private async Task ValidateMaterialItemAsync(MaterialItemEditViewModel model, int? currentItemId)
	{
		if (!string.IsNullOrWhiteSpace(model.ItemCode) && await _context.MaterialItems.IgnoreQueryFilters().AnyAsync((MaterialItem item) => !item.IsDeleted && item.ItemCategory == model.Category && item.ItemCode == model.ItemCode.Trim() && (!((int?)currentItemId).HasValue || item.Id != ((int?)currentItemId).Value)))
		{
			base.ModelState.AddModelError("ItemCode", "产品编号已存在。");
		}
		foreach (MaterialItemBomLineInputModel line in model.BomLines.Where((MaterialItemBomLineInputModel materialItemBomLineInputModel) => !materialItemBomLineInputModel.IsDeleted && IsBomLineFilled(materialItemBomLineInputModel)))
		{
			if (string.IsNullOrWhiteSpace(line.ComponentCode) || string.IsNullOrWhiteSpace(line.ComponentName))
			{
				base.ModelState.AddModelError("BomLines", "已填写内容的 BOM 行必须输入物料编号和物料名称。");
				break;
			}
		}
	}

	private static void ApplyItemValues(MaterialItem item, MaterialItemEditViewModel model)
	{
		item.ItemCategory = NormalizeCategory(model.Category);
		item.ItemCode = model.ItemCode.Trim();
		item.Barcode = NormalizeText(model.Barcode);
		item.ItemName = model.ItemName.Trim();
		item.Status = model.Status.Trim();
		item.ItemType = NormalizeText(model.ItemType);
		item.Brand = NormalizeText(model.Brand);
		item.Model = NormalizeText(model.Model);
		item.Specification = NormalizeText(model.Specification);
		item.Origin = NormalizeText(model.Origin);
		item.BaseUnit = NormalizeText(model.BaseUnit);
		item.PurchaseUnit = NormalizeText(model.PurchaseUnit);
		item.InventoryUnit = NormalizeText(model.InventoryUnit);
		item.SalesUnit = NormalizeText(model.SalesUnit);
		item.PurchaseUnitRate = model.PurchaseUnitRate;
		item.InventoryUnitRate = model.InventoryUnitRate;
		item.SalesUnitRate = model.SalesUnitRate;
		item.InventoryQuantity = model.InventoryQuantity;
		item.InventoryCapacity = model.InventoryCapacity;
		item.PrimaryLocation = NormalizeText(model.PrimaryLocation);
		item.CurrentCost = model.CurrentCost;
		item.SuggestedPrice = model.SuggestedPrice;
		item.ProductForm = model.ProductForm;
		item.DetailDescription = NormalizeText(model.DetailDescription);
		item.ForeignName = NormalizeText(model.ForeignName);
		item.ForeignDescription = NormalizeText(model.ForeignDescription);
		item.SupplierItemCode = NormalizeText(model.SupplierItemCode);
		item.CustomsCode = NormalizeText(model.CustomsCode);
		item.InternationalBarcode = NormalizeText(model.InternationalBarcode);
		item.MaterialTexture = NormalizeText(model.MaterialTexture);
		item.Feature = NormalizeText(model.Feature);
		item.CustomField1 = NormalizeText(model.CustomField1);
		item.CustomField2 = NormalizeText(model.CustomField2);
		item.CustomDate3 = model.CustomDate3;
		item.CustomField4 = NormalizeText(model.CustomField4);
		item.CustomField5 = NormalizeText(model.CustomField5);
		item.CustomField6 = NormalizeText(model.CustomField6);
		item.CustomField7 = NormalizeText(model.CustomField7);
		item.CustomField8 = NormalizeText(model.CustomField8);
		item.CustomField9 = NormalizeText(model.CustomField9);
		item.Website = NormalizeText(model.Website);
		item.Remarks = NormalizeText(model.Remarks);
		item.ArchivePath = NormalizeText(model.ArchivePath);
		item.SinglePackageQuantity = model.SinglePackageQuantity;
		item.InnerPackageQuantity = model.InnerPackageQuantity;
		item.CartonPackageQuantity = model.CartonPackageQuantity;
		item.OtherPackageQuantity = model.OtherPackageQuantity;
		item.SinglePackageSize = NormalizeText(model.SinglePackageSize);
		item.InnerPackageSize = NormalizeText(model.InnerPackageSize);
		item.CartonPackageSize = NormalizeText(model.CartonPackageSize);
		item.OtherPackageSize = NormalizeText(model.OtherPackageSize);
		item.SingleCbm = model.SingleCbm;
		item.InnerCbm = model.InnerCbm;
		item.CartonCbm = model.CartonCbm;
		item.OtherCbm = model.OtherCbm;
		item.SingleNetWeight = model.SingleNetWeight;
		item.InnerNetWeight = model.InnerNetWeight;
		item.CartonNetWeight = model.CartonNetWeight;
		item.OtherNetWeight = model.OtherNetWeight;
		item.SingleGrossWeight = model.SingleGrossWeight;
		item.InnerGrossWeight = model.InnerGrossWeight;
		item.CartonGrossWeight = model.CartonGrossWeight;
		item.OtherGrossWeight = model.OtherGrossWeight;
		item.ShippingMark = NormalizeText(model.ShippingMark);
		item.EnableSerialNumber = model.EnableSerialNumber;
		item.EnableBatch = model.EnableBatch;
		item.EnableShelfLife = model.EnableShelfLife;
		item.ShelfLifeDays = model.ShelfLifeDays;
		item.EnableMaintenancePeriod = model.EnableMaintenancePeriod;
		item.MaintenancePeriodDays = model.MaintenancePeriodDays;
	}

	private void ApplyBomRows(MaterialItem item, MaterialItemEditViewModel model, List<MaterialItemBomLine> existingLines)
	{
		Dictionary<int, MaterialItemBomLine> dictionary = existingLines.ToDictionary((MaterialItemBomLine line) => line.Id);
		List<MaterialItemBomLineInputModel> list = model.BomLines.Where((MaterialItemBomLineInputModel line) => line.Id.HasValue || line.IsDeleted || IsBomLineFilled(line)).ToList();
		int num = 1;
		foreach (MaterialItemBomLineInputModel item2 in list)
		{
			if (item2.Id.HasValue && dictionary.TryGetValue(item2.Id.Value, out var value))
			{
				if (item2.IsDeleted)
				{
					MarkLineDeleted(value);
					continue;
				}
				ApplyBomValues(value, item2, num++);
				value.UpdatedBy = GetCurrentUserName();
				value.UpdatedAt = DateTime.Now;
			}
			else if (!item2.IsDeleted && IsBomLineFilled(item2))
			{
				MaterialItemBomLine materialItemBomLine = new MaterialItemBomLine
				{
					CreatedBy = GetCurrentUserName(),
					CreatedAt = DateTime.Now
				};
				ApplyBomValues(materialItemBomLine, item2, num++);
				item.BomLines.Add(materialItemBomLine);
			}
		}
	}

	private void ApplyPriceRows(MaterialItem item, MaterialItemEditViewModel model, List<MaterialItemPriceLine> existingLines)
	{
		Dictionary<int, MaterialItemPriceLine> dictionary = existingLines.ToDictionary((MaterialItemPriceLine line) => line.Id);
		List<MaterialItemPriceLineInputModel> list = model.PriceLines.Where((MaterialItemPriceLineInputModel line) => line.Id.HasValue || line.IsDeleted || IsPriceLineFilled(line)).ToList();
		int num = 1;
		foreach (MaterialItemPriceLineInputModel item2 in list)
		{
			if (item2.Id.HasValue && dictionary.TryGetValue(item2.Id.Value, out var value))
			{
				if (item2.IsDeleted)
				{
					MarkLineDeleted(value);
					continue;
				}
				ApplyPriceValues(value, item2, num++);
				value.UpdatedBy = GetCurrentUserName();
				value.UpdatedAt = DateTime.Now;
			}
			else if (!item2.IsDeleted && IsPriceLineFilled(item2))
			{
				MaterialItemPriceLine materialItemPriceLine = new MaterialItemPriceLine
				{
					CreatedBy = GetCurrentUserName(),
					CreatedAt = DateTime.Now
				};
				ApplyPriceValues(materialItemPriceLine, item2, num++);
				item.PriceLines.Add(materialItemPriceLine);
			}
		}
	}

	private static void ApplyBomValues(MaterialItemBomLine line, MaterialItemBomLineInputModel row, int sortOrder)
	{
		line.ComponentCode = row.ComponentCode?.Trim() ?? string.Empty;
		line.ComponentName = row.ComponentName?.Trim() ?? string.Empty;
		line.ComponentDescription = NormalizeText(row.ComponentDescription);
		line.Quantity = row.Quantity;
		line.LossRatePercent = row.LossRatePercent;
		line.QuantityWithLoss = row.QuantityWithLoss;
		line.Unit = NormalizeText(row.Unit);
		line.CostPrice = row.CostPrice;
		line.CostAmount = row.CostAmount;
		line.Remarks = NormalizeText(row.Remarks);
		line.SortOrder = sortOrder;
		line.IsDeleted = false;
	}

	private static void ApplyPriceValues(MaterialItemPriceLine line, MaterialItemPriceLineInputModel row, int sortOrder)
	{
		line.PriceCategory = row.PriceCategory?.Trim() ?? string.Empty;
		line.Quantity = row.Quantity;
		line.Price = row.Price;
		line.Currency = row.Currency;
		line.SortOrder = sortOrder;
		line.IsDeleted = false;
	}

	private static MaterialItemEditViewModel ToEditModel(MaterialItem item)
	{
		MaterialItemEditViewModel materialItemEditViewModel = new MaterialItemEditViewModel();
		materialItemEditViewModel.Id = item.Id;
		materialItemEditViewModel.Category = item.ItemCategory;
		materialItemEditViewModel.ItemCode = item.ItemCode;
		materialItemEditViewModel.Barcode = item.Barcode;
		materialItemEditViewModel.ItemName = item.ItemName;
		materialItemEditViewModel.Status = item.Status;
		materialItemEditViewModel.ItemType = item.ItemType ?? "电子";
		materialItemEditViewModel.Brand = item.Brand;
		materialItemEditViewModel.Model = item.Model;
		materialItemEditViewModel.Specification = item.Specification;
		materialItemEditViewModel.Origin = item.Origin;
		materialItemEditViewModel.BaseUnit = item.BaseUnit ?? "部";
		materialItemEditViewModel.PurchaseUnit = item.PurchaseUnit ?? "箱";
		materialItemEditViewModel.InventoryUnit = item.InventoryUnit ?? "部";
		materialItemEditViewModel.SalesUnit = item.SalesUnit ?? "部";
		materialItemEditViewModel.PurchaseUnitRate = item.PurchaseUnitRate;
		materialItemEditViewModel.InventoryUnitRate = item.InventoryUnitRate;
		materialItemEditViewModel.SalesUnitRate = item.SalesUnitRate;
		materialItemEditViewModel.InventoryQuantity = item.InventoryQuantity;
		materialItemEditViewModel.InventoryCapacity = item.InventoryCapacity;
		materialItemEditViewModel.PrimaryLocation = item.PrimaryLocation;
		materialItemEditViewModel.CurrentCost = item.CurrentCost;
		materialItemEditViewModel.SuggestedPrice = item.SuggestedPrice;
		materialItemEditViewModel.ProductForm = item.ProductForm;
		materialItemEditViewModel.DetailDescription = item.DetailDescription;
		materialItemEditViewModel.ForeignName = item.ForeignName;
		materialItemEditViewModel.ForeignDescription = item.ForeignDescription;
		materialItemEditViewModel.SupplierItemCode = item.SupplierItemCode;
		materialItemEditViewModel.CustomsCode = item.CustomsCode;
		materialItemEditViewModel.InternationalBarcode = item.InternationalBarcode;
		materialItemEditViewModel.MaterialTexture = item.MaterialTexture;
		materialItemEditViewModel.Feature = item.Feature;
		materialItemEditViewModel.CustomField1 = item.CustomField1;
		materialItemEditViewModel.CustomField2 = item.CustomField2;
		materialItemEditViewModel.CustomDate3 = item.CustomDate3;
		materialItemEditViewModel.CustomField4 = item.CustomField4;
		materialItemEditViewModel.CustomField5 = item.CustomField5;
		materialItemEditViewModel.CustomField6 = item.CustomField6;
		materialItemEditViewModel.CustomField7 = item.CustomField7;
		materialItemEditViewModel.CustomField8 = item.CustomField8;
		materialItemEditViewModel.CustomField9 = item.CustomField9;
		materialItemEditViewModel.Website = item.Website;
		materialItemEditViewModel.Remarks = item.Remarks;
		materialItemEditViewModel.ArchivePath = item.ArchivePath;
		materialItemEditViewModel.SinglePackageQuantity = item.SinglePackageQuantity;
		materialItemEditViewModel.InnerPackageQuantity = item.InnerPackageQuantity;
		materialItemEditViewModel.CartonPackageQuantity = item.CartonPackageQuantity;
		materialItemEditViewModel.OtherPackageQuantity = item.OtherPackageQuantity;
		materialItemEditViewModel.SinglePackageSize = item.SinglePackageSize;
		materialItemEditViewModel.InnerPackageSize = item.InnerPackageSize;
		materialItemEditViewModel.CartonPackageSize = item.CartonPackageSize;
		materialItemEditViewModel.OtherPackageSize = item.OtherPackageSize;
		materialItemEditViewModel.SingleCbm = item.SingleCbm;
		materialItemEditViewModel.InnerCbm = item.InnerCbm;
		materialItemEditViewModel.CartonCbm = item.CartonCbm;
		materialItemEditViewModel.OtherCbm = item.OtherCbm;
		materialItemEditViewModel.SingleNetWeight = item.SingleNetWeight;
		materialItemEditViewModel.InnerNetWeight = item.InnerNetWeight;
		materialItemEditViewModel.CartonNetWeight = item.CartonNetWeight;
		materialItemEditViewModel.OtherNetWeight = item.OtherNetWeight;
		materialItemEditViewModel.SingleGrossWeight = item.SingleGrossWeight;
		materialItemEditViewModel.InnerGrossWeight = item.InnerGrossWeight;
		materialItemEditViewModel.CartonGrossWeight = item.CartonGrossWeight;
		materialItemEditViewModel.OtherGrossWeight = item.OtherGrossWeight;
		materialItemEditViewModel.ShippingMark = item.ShippingMark;
		materialItemEditViewModel.EnableSerialNumber = item.EnableSerialNumber;
		materialItemEditViewModel.EnableBatch = item.EnableBatch;
		materialItemEditViewModel.EnableShelfLife = item.EnableShelfLife;
		materialItemEditViewModel.ShelfLifeDays = item.ShelfLifeDays;
		materialItemEditViewModel.EnableMaintenancePeriod = item.EnableMaintenancePeriod;
		materialItemEditViewModel.MaintenancePeriodDays = item.MaintenancePeriodDays;
		materialItemEditViewModel.BomLines = (from line in item.BomLines
			orderby line.SortOrder, line.Id
			select line).Select(ToBomInputModel).ToList();
		materialItemEditViewModel.PriceLines = (from line in item.PriceLines
			orderby line.SortOrder, line.Id
			select line).Select(ToPriceInputModel).ToList();
		return materialItemEditViewModel;
	}

	private static MaterialItemListItemViewModel ToListItem(MaterialItem item, int sequenceNo)
	{
		return new MaterialItemListItemViewModel
		{
			Id = item.Id,
			SequenceNo = sequenceNo,
			ItemCode = item.ItemCode,
			ItemName = item.ItemName,
			Status = item.Status,
			Description = (item.DetailDescription ?? item.ItemName),
			ItemType = (item.ItemType ?? string.Empty),
			Brand = (item.Brand ?? string.Empty),
			SpecificationModel = BuildSpecificationModel(item),
			Origin = (item.Origin ?? string.Empty),
			Unit = (item.BaseUnit ?? string.Empty),
			Inventory = FormatQuantity(item.InventoryQuantity),
			Remarks = (item.Remarks ?? string.Empty),
			LastModifiedBy = (item.UpdatedBy ?? item.CreatedBy ?? string.Empty),
			LastModifiedAt = (item.UpdatedAt ?? item.CreatedAt).ToString("yyyy-MM-dd HH:mm")
		};
	}

	private Task<MaterialItem?> LoadDetailItemAsync(int id, bool asNoTracking = false)
	{
		IQueryable<MaterialItem> queryable = from item in _context.MaterialItems.Include((MaterialItem item) => item.BomLines).Include((MaterialItem item) => item.PriceLines)
			where item.Id == id
			select item;
		IQueryable<MaterialItem> source;
		if (!asNoTracking)
		{
			source = queryable;
		}
		else
		{
			IQueryable<MaterialItem> queryable2 = queryable.AsNoTracking();
			source = queryable2;
		}
		return source.FirstOrDefaultAsync();
	}

	private static List<MaterialItemRelatedRecordViewModel> BuildRelatedRecords(MaterialItem? selectedItem)
	{
		if (selectedItem == null)
		{
			return new List<MaterialItemRelatedRecordViewModel>();
		}
		int num = 1;
		List<MaterialItemRelatedRecordViewModel> list = new List<MaterialItemRelatedRecordViewModel>(num);
		CollectionsMarshal.SetCount(list, num);
		CollectionsMarshal.AsSpan(list)[0] = new MaterialItemRelatedRecordViewModel
		{
			SequenceNo = 1,
			DocumentNumber = "HYTPO23120101",
			DocumentDate = "2023-12-05",
			Status = "进行中",
			PartnerCode = "V001",
			PartnerName = "供应商名称1",
			Quantity = "30",
			Unit = (selectedItem.BaseUnit ?? "部"),
			UnitPrice = "3,000.00",
			Amount = "90,000.00",
			Remarks = "随意填写备注"
		};
		return list;
	}

	private static MaterialItemEditViewModel NormalizeDetailRows(MaterialItemEditViewModel model)
	{
		model.BomLines = model.BomLines.Where((MaterialItemBomLineInputModel line) => line.Id.HasValue || line.IsDeleted || IsBomLineFilled(line)).ToList();
		model.PriceLines = model.PriceLines.Where((MaterialItemPriceLineInputModel line) => line.Id.HasValue || line.IsDeleted || IsPriceLineFilled(line)).ToList();
		return model;
	}

	private static bool IsBomLineFilled(MaterialItemBomLineInputModel line)
	{
		return !string.IsNullOrWhiteSpace(line.ComponentCode) || !string.IsNullOrWhiteSpace(line.ComponentName) || !string.IsNullOrWhiteSpace(line.ComponentDescription) || line.Quantity != 0m || line.LossRatePercent != 0m || line.QuantityWithLoss != 0m || line.CostPrice != 0m || line.CostAmount != 0m || !string.IsNullOrWhiteSpace(line.Remarks);
	}

	private static bool IsPriceLineFilled(MaterialItemPriceLineInputModel line)
	{
		return !string.IsNullOrWhiteSpace(line.PriceCategory) || line.Quantity != 0m || line.Price != 0m;
	}

	private void MarkLineDeleted(BaseEntity line)
	{
		line.IsDeleted = true;
		line.UpdatedBy = GetCurrentUserName();
		line.UpdatedAt = DateTime.Now;
	}

	private static MaterialItemBomLineInputModel ToBomInputModel(MaterialItemBomLine line)
	{
		return new MaterialItemBomLineInputModel
		{
			Id = line.Id,
			ComponentCode = line.ComponentCode,
			ComponentName = line.ComponentName,
			ComponentDescription = line.ComponentDescription,
			Quantity = line.Quantity,
			LossRatePercent = line.LossRatePercent,
			QuantityWithLoss = line.QuantityWithLoss,
			Unit = line.Unit,
			CostPrice = line.CostPrice,
			CostAmount = line.CostAmount,
			Remarks = line.Remarks
		};
	}

	private static MaterialItemPriceLineInputModel ToPriceInputModel(MaterialItemPriceLine line)
	{
		return new MaterialItemPriceLineInputModel
		{
			Id = line.Id,
			PriceCategory = line.PriceCategory,
			Quantity = line.Quantity,
			Price = line.Price,
			Currency = line.Currency
		};
	}

	private RedirectToActionResult RedirectToCategoryList(string category, int? selectedItemId = null)
	{
		return RedirectToAction(GetCategoryAction(category), new { selectedItemId });
	}

	private static string NormalizeCategory(string? category)
	{
		if (1 == 0)
		{
		}
		string result = category switch
		{
			"材料" => "材料", 
			"辅料" => "辅料", 
			"资产资料" => "资产资料", 
			"办公用品" => "办公用品", 
			_ => "产品", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string GetPageTitle(string category)
	{
		if (1 == 0)
		{
		}
		string result = category switch
		{
			"材料" => "材料资料", 
			"辅料" => "辅料资料", 
			"资产资料" => "资产资料", 
			"办公用品" => "办公用品", 
			_ => "产品资料", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string GetCategoryAction(string category)
	{
		if (1 == 0)
		{
		}
		string result = ((category == "材料") ? "Materials" : ((!(category == "辅料")) ? "Products" : "AuxiliaryMaterials"));
		if (1 == 0)
		{
		}
		return result;
	}

	private static string TrimCategorySuffix(string pageTitle)
	{
		string result;
		if (!pageTitle.EndsWith("资料", StringComparison.Ordinal))
		{
			result = pageTitle;
		}
		else
		{
			result = pageTitle.Substring(0, pageTitle.Length - 2);
		}
		return result;
	}

	private static string BuildSpecificationModel(MaterialItem item)
	{
		return string.Join(" / ", new string[2] { item.Specification, item.Model }.Where((string value) => !string.IsNullOrWhiteSpace(value)));
	}

	private static string FormatQuantity(decimal value)
	{
		return value.ToString("0.####");
	}

	private static string? NormalizeText(string? value)
	{
		return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	}

	private static string EscapeCsv(string? value)
	{
		string text = value ?? string.Empty;
		return "\"" + text.Replace("\"", "\"\"") + "\"";
	}

	private ContentResult BuildDetailPageCloseResult()
	{
		string content = "<!DOCTYPE html>\n<html lang=\"zh-CN\">\n<head>\n    <meta charset=\"utf-8\" />\n    <title>处理完成</title>\n</head>\n<body>\n    <script>\n        const closeMessage = {\n            type: \"open-erp:employee-modal-close\",\n            refreshRequested: true\n        };\n\n        if (window.parent && window.parent !== window) {\n            window.parent.postMessage(closeMessage, window.location.origin);\n        } else if (window.opener && !window.opener.closed) {\n            window.opener.postMessage(closeMessage, window.location.origin);\n            window.close();\n        }\n    </script>\n</body>\n</html>";
		return Content(content, "text/html; charset=utf-8");
	}

	private string GetCurrentUserName()
	{
		return base.User.Identity?.Name ?? "系统";
	}
}
