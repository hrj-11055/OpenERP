using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenERP.Logistics.Data;
using OpenERP.Logistics.Models.Entities;
using OpenERP.Web.Areas.Logistics.ViewModels.WorkshopWarehouses;

namespace OpenERP.Web.Areas.Logistics.Controllers;

[Authorize]
[Area("Logistics")]
public class WorkshopWarehousesController : Controller
{
	private readonly ApplicationDbContext _context;

	public WorkshopWarehousesController(ApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<IActionResult> Index(string? scope, string? keyword, int? selectedWarehouseId)
	{
		string queryScope = (string.IsNullOrWhiteSpace(scope) ? "全部" : scope.Trim());
		string queryKeyword = keyword?.Trim();
		IQueryable<WorkshopWarehouse> warehouseQuery = ApplyWarehouseSearch(_context.WorkshopWarehouses.AsNoTracking(), queryScope, queryKeyword);
		List<WorkshopWarehouse> warehouses = await warehouseQuery.OrderBy((WorkshopWarehouse warehouse) => warehouse.WarehouseCode).ToListAsync();
		WorkshopWarehouse selectedWarehouse = (selectedWarehouseId.HasValue ? warehouses.FirstOrDefault((WorkshopWarehouse warehouse) => warehouse.Id == selectedWarehouseId.Value) : warehouses.FirstOrDefault());
		List<WarehouseLocation> list = ((selectedWarehouse != null) ? (await (from location in _context.WarehouseLocations.AsNoTracking()
			where location.WorkshopWarehouseId == selectedWarehouse.Id
			orderby location.SortOrder, location.LocationCode
			select location).ToListAsync()) : new List<WarehouseLocation>());
		List<WarehouseLocation> locations = list;
		WorkshopWarehouseIndexViewModel model = new WorkshopWarehouseIndexViewModel
		{
			Scope = queryScope,
			Keyword = queryKeyword,
			SelectedWarehouseId = selectedWarehouse?.Id,
			SelectedWarehouseName = ((selectedWarehouse == null) ? "未选择仓库" : (selectedWarehouse.WarehouseCode + " " + selectedWarehouse.WarehouseName)),
			Warehouses = warehouses.Select((WorkshopWarehouse warehouse, int index) => ToWarehouseListItem(warehouse, index + 1)).ToList(),
			Locations = locations.Select((WarehouseLocation location, int index) => ToLocationListItem(location, index + 1)).ToList()
		};
		return View(model);
	}

	public IActionResult Create(bool popup = false)
	{
		base.ViewBag.IsPopup = popup;
		return View("Edit", new WorkshopWarehouseEditViewModel());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(WorkshopWarehouseEditViewModel model, bool popup = false)
	{
		await ValidateWorkshopWarehouseAsync(model, null);
		if (!base.ModelState.IsValid)
		{
			base.ViewBag.IsPopup = popup;
			return View("Edit", NormalizeLocationRows(model));
		}
		WorkshopWarehouse warehouse = new WorkshopWarehouse();
		ApplyWarehouseValues(warehouse, model);
		warehouse.CreatedBy = GetCurrentUserName();
		warehouse.CreatedAt = DateTime.Now;
		ApplyLocationRows(warehouse, model, new List<WarehouseLocation>());
		_context.WorkshopWarehouses.Add(warehouse);
		await _context.SaveChangesAsync();
		IActionResult result;
		if (!popup)
		{
			IActionResult actionResult = RedirectToAction("Edit", new
			{
				id = warehouse.Id
			});
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
		WorkshopWarehouse warehouse = await _context.WorkshopWarehouses.Include((WorkshopWarehouse item) => item.Locations).FirstOrDefaultAsync((WorkshopWarehouse item) => item.Id == id);
		if (warehouse == null)
		{
			return NotFound();
		}
		base.ViewBag.IsPopup = popup;
		return View(ToEditModel(warehouse));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(int id, WorkshopWarehouseEditViewModel model, bool popup = false)
	{
		if (model.Id != id)
		{
			return NotFound();
		}
		WorkshopWarehouse warehouse = await _context.WorkshopWarehouses.Include((WorkshopWarehouse item) => item.Locations).FirstOrDefaultAsync((WorkshopWarehouse item) => item.Id == id);
		if (warehouse == null)
		{
			return NotFound();
		}
		await ValidateWorkshopWarehouseAsync(model, id);
		if (!base.ModelState.IsValid)
		{
			base.ViewBag.IsPopup = popup;
			return View(NormalizeLocationRows(model));
		}
		ApplyWarehouseValues(warehouse, model);
		warehouse.UpdatedBy = GetCurrentUserName();
		warehouse.UpdatedAt = DateTime.Now;
		ApplyLocationRows(warehouse, model, warehouse.Locations.ToList());
		await _context.SaveChangesAsync();
		IActionResult result;
		if (!popup)
		{
			IActionResult actionResult = RedirectToAction("Edit", new
			{
				id = warehouse.Id
			});
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
		WorkshopWarehouse warehouse = await _context.WorkshopWarehouses.Include((WorkshopWarehouse item) => item.Locations).AsNoTracking().FirstOrDefaultAsync((WorkshopWarehouse item) => item.Id == id);
		if (warehouse == null)
		{
			return NotFound();
		}
		base.ViewBag.IsPopup = popup;
		base.ViewBag.IsReadOnly = true;
		return View("Edit", ToEditModel(warehouse));
	}

	public async Task<IActionResult> Copy(int id, bool popup = false)
	{
		WorkshopWarehouse warehouse = await _context.WorkshopWarehouses.Include((WorkshopWarehouse item) => item.Locations).AsNoTracking().FirstOrDefaultAsync((WorkshopWarehouse item) => item.Id == id);
		if (warehouse == null)
		{
			return NotFound();
		}
		WorkshopWarehouseEditViewModel model = ToEditModel(warehouse);
		model.Id = null;
		model.WarehouseCode += "_COPY";
		model.WarehouseName += " 副本";
		foreach (WarehouseLocationInputModel location in model.Locations)
		{
			location.Id = null;
		}
		base.ViewBag.IsPopup = popup;
		return View("Edit", model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Delete(int[] ids)
	{
		int[] warehouseIds = ids.Where((int id) => id > 0).Distinct().ToArray();
		if (warehouseIds.Length == 0)
		{
			return BadRequest("请先选择要删除的仓库资料。");
		}
		List<WorkshopWarehouse> warehouses = await (from item in _context.WorkshopWarehouses.Include((WorkshopWarehouse item) => item.Locations)
			where warehouseIds.Contains(item.Id) && !item.IsDeleted
			select item).ToListAsync();
		if (warehouses.Count == 0)
		{
			return NotFound();
		}
		string currentUserName = GetCurrentUserName();
		DateTime deletedAt = DateTime.Now;
		foreach (WorkshopWarehouse warehouse in warehouses)
		{
			warehouse.IsDeleted = true;
			warehouse.UpdatedBy = currentUserName;
			warehouse.UpdatedAt = deletedAt;
			foreach (WarehouseLocation location in warehouse.Locations)
			{
				location.IsDeleted = true;
				location.UpdatedBy = currentUserName;
				location.UpdatedAt = deletedAt;
			}
		}
		await _context.SaveChangesAsync();
		return RedirectToAction("Index");
	}

	public async Task<IActionResult> Export(string? scope, string? keyword)
	{
		List<WorkshopWarehouse> warehouses = await (from workshopWarehouse in ApplyWarehouseSearch(scope: string.IsNullOrWhiteSpace(scope) ? "全部" : scope.Trim(), keyword: keyword?.Trim(), query: _context.WorkshopWarehouses.AsNoTracking())
			orderby workshopWarehouse.WarehouseCode
			select workshopWarehouse).ToListAsync();
		StringBuilder csvBuilder = new StringBuilder();
		csvBuilder.AppendLine("仓库编号,仓库名称,状态,类型,负责人,地区,地址,电话,传真,电邮,备注");
		foreach (WorkshopWarehouse warehouse in warehouses)
		{
			string[] row = new string[11]
			{
				warehouse.WarehouseCode,
				warehouse.WarehouseName,
				warehouse.Status,
				warehouse.WarehouseType,
				warehouse.Manager,
				BuildRegion(warehouse),
				warehouse.Address,
				warehouse.Phone,
				warehouse.Fax,
				warehouse.Email,
				warehouse.Remarks
			};
			csvBuilder.AppendLine(string.Join(",", row.Select(EscapeCsv)));
		}
		byte[] bytes = Encoding.UTF8.GetBytes("\ufeff" + csvBuilder);
		return File(bytes, "text/csv; charset=utf-8", $"车间仓库资料_{DateTime.Now:yyyyMMddHHmmss}.csv");
	}

	private static IQueryable<WorkshopWarehouse> ApplyWarehouseSearch(IQueryable<WorkshopWarehouse> query, string scope, string? keyword)
	{
		if (string.IsNullOrWhiteSpace(keyword))
		{
			return query;
		}
		if (1 == 0)
		{
		}
		IQueryable<WorkshopWarehouse> result = scope switch
		{
			"仓库编号" => query.Where((WorkshopWarehouse warehouse) => warehouse.WarehouseCode.Contains(keyword)), 
			"仓库名称" => query.Where((WorkshopWarehouse warehouse) => warehouse.WarehouseName.Contains(keyword)), 
			"负责人" => query.Where((WorkshopWarehouse warehouse) => warehouse.Manager != null && warehouse.Manager.Contains(keyword)), 
			"备注" => query.Where((WorkshopWarehouse warehouse) => warehouse.Remarks != null && warehouse.Remarks.Contains(keyword)), 
			_ => query.Where((WorkshopWarehouse warehouse) => warehouse.WarehouseCode.Contains(keyword) || warehouse.WarehouseName.Contains(keyword) || (warehouse.Status != null && warehouse.Status.Contains(keyword)) || (warehouse.WarehouseType != null && warehouse.WarehouseType.Contains(keyword)) || (warehouse.Manager != null && warehouse.Manager.Contains(keyword)) || (warehouse.CountryRegion != null && warehouse.CountryRegion.Contains(keyword)) || (warehouse.City != null && warehouse.City.Contains(keyword)) || (warehouse.District != null && warehouse.District.Contains(keyword)) || (warehouse.Address != null && warehouse.Address.Contains(keyword)) || (warehouse.Phone != null && warehouse.Phone.Contains(keyword)) || (warehouse.Fax != null && warehouse.Fax.Contains(keyword)) || (warehouse.Email != null && warehouse.Email.Contains(keyword)) || (warehouse.Remarks != null && warehouse.Remarks.Contains(keyword))), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private async Task ValidateWorkshopWarehouseAsync(WorkshopWarehouseEditViewModel model, int? currentWarehouseId)
	{
		if (!string.IsNullOrWhiteSpace(model.WarehouseCode) && await _context.WorkshopWarehouses.AnyAsync((WorkshopWarehouse warehouse) => warehouse.WarehouseCode == model.WarehouseCode.Trim() && (!((int?)currentWarehouseId).HasValue || warehouse.Id != ((int?)currentWarehouseId).Value)))
		{
			base.ModelState.AddModelError("WarehouseCode", "仓库编号已存在");
		}
		List<WarehouseLocationInputModel> activeLocations = (from location in model.Locations
			where !location.IsDeleted
			where !string.IsNullOrWhiteSpace(location.LocationCode)
			select location).ToList();
		IGrouping<string, WarehouseLocationInputModel> duplicateLocationCode = activeLocations.GroupBy<WarehouseLocationInputModel, string>((WarehouseLocationInputModel location) => location.LocationCode.Trim(), StringComparer.OrdinalIgnoreCase).FirstOrDefault((IGrouping<string, WarehouseLocationInputModel> group) => group.Count() > 1);
		if (duplicateLocationCode != null)
		{
			base.ModelState.AddModelError("Locations", "库位编号“" + duplicateLocationCode.Key + "”重复");
		}
	}

	private static void ApplyWarehouseValues(WorkshopWarehouse warehouse, WorkshopWarehouseEditViewModel model)
	{
		warehouse.WarehouseCode = model.WarehouseCode.Trim();
		warehouse.WarehouseName = model.WarehouseName.Trim();
		warehouse.Status = model.Status.Trim();
		warehouse.WarehouseType = NormalizeText(model.WarehouseType);
		warehouse.Manager = NormalizeText(model.Manager);
		warehouse.CountryRegion = NormalizeText(model.CountryRegion);
		warehouse.City = NormalizeText(model.City);
		warehouse.District = NormalizeText(model.District);
		warehouse.Address = NormalizeText(model.Address);
		warehouse.Phone = NormalizeText(model.Phone);
		warehouse.Fax = NormalizeText(model.Fax);
		warehouse.Email = NormalizeText(model.Email);
		warehouse.Remarks = NormalizeText(model.Remarks);
	}

	private void ApplyLocationRows(WorkshopWarehouse warehouse, WorkshopWarehouseEditViewModel model, List<WarehouseLocation> existingLocations)
	{
		Dictionary<int, WarehouseLocation> dictionary = existingLocations.ToDictionary((WarehouseLocation location) => location.Id);
		int num = 1;
		foreach (WarehouseLocationInputModel location in model.Locations)
		{
			if (location.Id.HasValue && dictionary.TryGetValue(location.Id.Value, out var value))
			{
				if (location.IsDeleted)
				{
					value.IsDeleted = true;
					value.UpdatedBy = GetCurrentUserName();
					value.UpdatedAt = DateTime.Now;
				}
				else
				{
					ApplyLocationValues(value, location, num++);
					value.UpdatedBy = GetCurrentUserName();
					value.UpdatedAt = DateTime.Now;
				}
			}
			else if (!location.IsDeleted && !string.IsNullOrWhiteSpace(location.LocationCode) && !string.IsNullOrWhiteSpace(location.LocationName))
			{
				WarehouseLocation warehouseLocation = new WarehouseLocation
				{
					CreatedBy = GetCurrentUserName(),
					CreatedAt = DateTime.Now
				};
				ApplyLocationValues(warehouseLocation, location, num++);
				warehouse.Locations.Add(warehouseLocation);
			}
		}
	}

	private static void ApplyLocationValues(WarehouseLocation location, WarehouseLocationInputModel row, int sortOrder)
	{
		location.LocationCode = row.LocationCode.Trim();
		location.LocationName = row.LocationName.Trim();
		location.Status = row.Status.Trim();
		location.LocationLevel = NormalizeText(row.LocationLevel);
		location.Zone = NormalizeText(row.Zone);
		location.StorageEnvironment = NormalizeText(row.StorageEnvironment);
		location.Usage = NormalizeText(row.Usage);
		location.MaxLoadKg = row.MaxLoadKg;
		location.MaxVolumeCbm = row.MaxVolumeCbm;
		location.Remarks = NormalizeText(row.Remarks);
		location.SortOrder = sortOrder;
		location.IsDeleted = false;
	}

	private static WorkshopWarehouseEditViewModel ToEditModel(WorkshopWarehouse warehouse)
	{
		WorkshopWarehouseEditViewModel workshopWarehouseEditViewModel = new WorkshopWarehouseEditViewModel();
		workshopWarehouseEditViewModel.Id = warehouse.Id;
		workshopWarehouseEditViewModel.WarehouseCode = warehouse.WarehouseCode;
		workshopWarehouseEditViewModel.WarehouseName = warehouse.WarehouseName;
		workshopWarehouseEditViewModel.Status = warehouse.Status;
		workshopWarehouseEditViewModel.WarehouseType = warehouse.WarehouseType;
		workshopWarehouseEditViewModel.Manager = warehouse.Manager;
		workshopWarehouseEditViewModel.CountryRegion = warehouse.CountryRegion;
		workshopWarehouseEditViewModel.City = warehouse.City;
		workshopWarehouseEditViewModel.District = warehouse.District;
		workshopWarehouseEditViewModel.Address = warehouse.Address;
		workshopWarehouseEditViewModel.Phone = warehouse.Phone;
		workshopWarehouseEditViewModel.Fax = warehouse.Fax;
		workshopWarehouseEditViewModel.Email = warehouse.Email;
		workshopWarehouseEditViewModel.Remarks = warehouse.Remarks;
		workshopWarehouseEditViewModel.Locations = (from location in warehouse.Locations
			where !location.IsDeleted
			orderby location.SortOrder, location.LocationCode
			select location).Select(ToLocationInputModel).ToList();
		return workshopWarehouseEditViewModel;
	}

	private static WarehouseLocationInputModel ToLocationInputModel(WarehouseLocation location)
	{
		return new WarehouseLocationInputModel
		{
			Id = location.Id,
			LocationCode = location.LocationCode,
			LocationName = location.LocationName,
			Status = location.Status,
			LocationLevel = location.LocationLevel,
			Zone = location.Zone,
			StorageEnvironment = location.StorageEnvironment,
			Usage = location.Usage,
			MaxLoadKg = location.MaxLoadKg,
			MaxVolumeCbm = location.MaxVolumeCbm,
			Remarks = location.Remarks
		};
	}

	private static WorkshopWarehouseListItemViewModel ToWarehouseListItem(WorkshopWarehouse warehouse, int sequenceNo)
	{
		return new WorkshopWarehouseListItemViewModel
		{
			Id = warehouse.Id,
			SequenceNo = sequenceNo,
			WarehouseCode = warehouse.WarehouseCode,
			WarehouseName = warehouse.WarehouseName,
			Status = warehouse.Status,
			WarehouseType = (warehouse.WarehouseType ?? string.Empty),
			Manager = (warehouse.Manager ?? string.Empty),
			Region = BuildRegion(warehouse),
			Address = (warehouse.Address ?? string.Empty),
			Phone = (warehouse.Phone ?? string.Empty),
			Fax = (warehouse.Fax ?? string.Empty),
			Email = (warehouse.Email ?? string.Empty),
			Remarks = (warehouse.Remarks ?? string.Empty),
			LastModifiedBy = (warehouse.UpdatedBy ?? warehouse.CreatedBy ?? string.Empty),
			LastModifiedAt = (warehouse.UpdatedAt ?? warehouse.CreatedAt).ToString("yyyy-MM-dd HH:mm")
		};
	}

	private static WarehouseLocationListItemViewModel ToLocationListItem(WarehouseLocation location, int sequenceNo)
	{
		return new WarehouseLocationListItemViewModel
		{
			Id = location.Id,
			SequenceNo = sequenceNo,
			LocationCode = location.LocationCode,
			LocationName = location.LocationName,
			Status = location.Status,
			LocationLevel = (location.LocationLevel ?? string.Empty),
			Zone = (location.Zone ?? string.Empty),
			StorageEnvironment = (location.StorageEnvironment ?? string.Empty),
			Usage = (location.Usage ?? string.Empty),
			MaxLoadKg = location.MaxLoadKg,
			MaxVolumeCbm = location.MaxVolumeCbm,
			Remarks = (location.Remarks ?? string.Empty)
		};
	}

	private static WorkshopWarehouseEditViewModel NormalizeLocationRows(WorkshopWarehouseEditViewModel model)
	{
		model.Locations = model.Locations.Where((WarehouseLocationInputModel location) => location.Id.HasValue || location.IsDeleted || !string.IsNullOrWhiteSpace(location.LocationCode) || !string.IsNullOrWhiteSpace(location.LocationName) || !string.IsNullOrWhiteSpace(location.LocationLevel) || !string.IsNullOrWhiteSpace(location.Zone) || !string.IsNullOrWhiteSpace(location.StorageEnvironment) || !string.IsNullOrWhiteSpace(location.Usage) || location.MaxLoadKg != 0m || location.MaxVolumeCbm != 0m || !string.IsNullOrWhiteSpace(location.Remarks)).ToList();
		return model;
	}

	private static string BuildRegion(WorkshopWarehouse warehouse)
	{
		return string.Join(" ", new string[3] { warehouse.CountryRegion, warehouse.City, warehouse.District }.Where((string value) => !string.IsNullOrWhiteSpace(value)));
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
		string content = "<!DOCTYPE html>\n<html lang=\"zh-CN\">\n<head>\n    <meta charset=\"utf-8\" />\n    <title>处理中</title>\n</head>\n<body>\n    <script>\n        const closeMessage = {\n            type: \"open-erp:employee-modal-close\",\n            refreshRequested: true\n        };\n\n        if (window.parent && window.parent !== window) {\n            window.parent.postMessage(closeMessage, window.location.origin);\n        } else if (window.opener && !window.opener.closed) {\n            window.opener.postMessage(closeMessage, window.location.origin);\n            window.close();\n        }\n    </script>\n</body>\n</html>";
		return Content(content, "text/html; charset=utf-8");
	}

	private string GetCurrentUserName()
	{
		return base.User.Identity?.Name ?? "系统";
	}
}
