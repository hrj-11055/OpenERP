using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OpenERP.BasicData.Data;
using OpenERP.BasicData.Models;

namespace OpenERP.Web.Areas.BasicData.Controllers;

/// <summary>
/// 基础数据选项管理控制器（维护各基础数据类型下的选项字典）。
/// </summary>
[Area("BasicData")]
[Microsoft.AspNetCore.Authorization.Authorize]
    public class ItemsController : Controller
{
    /// <summary>
    /// 职位基础数据类型编码（供员工详情页快捷入口使用）。
    /// </summary>
    private const string PositionTypeCode = "POSITION";

    /// <summary>
    /// 民族基础数据类型编码（供员工详情页快捷入口使用）。
    /// </summary>
    private const string EthnicityTypeCode = "ETHNICITY";

    /// <summary>
    /// 职称基础数据类型编码（供员工详情页快捷入口使用）。
    /// </summary>
    private const string ProfessionalTitleTypeCode = "PROFESSIONAL_TITLE";

    /// <summary>
    /// 地域类型编码（二级地域字典，上级为国家地区）。
    /// </summary>
    private const string RegionTypeCode = "REGION";

    /// <summary>
    /// 国家地区类型编码（一级行政区字典）。
    /// </summary>
    private const string CountryRegionTypeCode = "COUNTRY_REGION";

    /// <summary>
    /// 城市类型编码（二级行政区字典）。
    /// </summary>
    private const string CityTypeCode = "CITY";

    /// <summary>
    /// 县域类型编码（三级行政区字典）。
    /// </summary>
    private const string CountyTypeCode = "COUNTY";

    /// <summary>
    /// 基础数据仓储（负责类型和选项数据读写）。
    /// </summary>
    private readonly IBasicDataRepository _repository;

    /// <summary>
    /// 行政区层级规则配置（定义各类型允许的上级类型）。
    /// </summary>
    private static readonly Dictionary<string, ParentRuleDefinition> LocationHierarchyRules =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [CountryRegionTypeCode] = new ParentRuleDefinition(
                ParentTypeCode: null,
                ParentRequired: false,
                ParentLabel: "上级选项",
                ParentPlaceholder: "国家地区是一级数据，不需要上级",
                ParentHint: "一级：国家地区"),
            [RegionTypeCode] = new ParentRuleDefinition(
                ParentTypeCode: CountryRegionTypeCode,
                ParentRequired: true,
                ParentLabel: "所属国家地区",
                ParentPlaceholder: "请选择国家地区",
                ParentHint: "二级：地域，上级必须是国家地区"),
            [CityTypeCode] = new ParentRuleDefinition(
                ParentTypeCode: CountryRegionTypeCode,
                ParentRequired: true,
                ParentLabel: "所属国家地区",
                ParentPlaceholder: "请选择国家地区",
                ParentHint: "二级：城市，上级必须是国家地区"),
            [CountyTypeCode] = new ParentRuleDefinition(
                ParentTypeCode: CityTypeCode,
                ParentRequired: true,
                ParentLabel: "所属城市",
                ParentPlaceholder: "请选择城市",
                ParentHint: "三级：县域，上级必须是城市")
        };

    public ItemsController(IBasicDataRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index(int? typeId, string? typeCode = null, bool popup = false)
    {
        typeId = await ResolveTypeIdAsync(typeId, typeCode);
        await PopulateTypeSelectListAsync(typeId);
        ViewBag.IsPopup = popup;
        string? selectedTypeCode = null;
        if (typeId.HasValue)
        {
            // 回传当前筛选类型名称，供页面标题显示业务上下文。
            var selectedType = await _repository.GetTypeByIdAsync(typeId.Value);
            selectedTypeCode = selectedType?.TypeCode;
            ViewData["CurrentTypeName"] = NormalizeTypeDisplayName(selectedType?.TypeCode, selectedType?.TypeName);
            ViewData["CurrentTypeCode"] = selectedTypeCode;
        }

        // 回传当前筛选类型ID，供“新增选项”默认带入。
        ViewData["CurrentTypeId"] = typeId;
        var items = await _repository.GetItemsAsync(typeId);
        if (string.Equals(selectedTypeCode, PositionTypeCode, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var item in items)
            {
                item.TypeName = NormalizeTypeDisplayName(selectedTypeCode, item.TypeName);
            }
        }

        return View(items);
    }

    public async Task<IActionResult> Create(int? typeId, string? typeCode = null, bool popup = false)
    {
        typeId = await ResolveTypeIdAsync(typeId, typeCode);
        ViewBag.IsPopup = popup;
        await PopulateEditorSelectDataAsync(typeId, null);
        return View(new BasicDataItem { TypeId = typeId ?? 0, IsActive = true, SortOrder = 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("TypeId,ItemCode,ItemName,ParentId,SortOrder,IsActive,Remark")] BasicDataItem input, bool popup = false)
    {
        await ValidateHierarchyAsync(input);
        if (!ModelState.IsValid)
        {
            ViewBag.IsPopup = popup;
            await PopulateEditorSelectDataAsync(input.TypeId, input.ParentId);
            return View(input);
        }

        await _repository.CreateItemAsync(input, User?.Identity?.Name);
        var createdItem = (await _repository.GetItemsByTypeIdAsync(input.TypeId))
            .FirstOrDefault(item => string.Equals(item.ItemCode, input.ItemCode, StringComparison.OrdinalIgnoreCase));

        TempData["BasicDataUpdatedTypeCode"] = await ResolveTypeCodeAsync(input.TypeId);
        TempData["BasicDataUpdatedSelectedId"] = createdItem?.Id ?? 0;
        return RedirectToAction(nameof(Index), new { typeId = input.TypeId, popup });
    }

    public async Task<IActionResult> Edit(int? id, bool popup = false)
    {
        if (id == null)
        {
            return NotFound();
        }

        var item = await _repository.GetItemByIdAsync(id.Value);
        if (item == null)
        {
            return NotFound();
        }

        ViewBag.IsPopup = popup;
        await PopulateEditorSelectDataAsync(item.TypeId, item.ParentId, item.Id);
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,TypeId,ItemCode,ItemName,ParentId,SortOrder,IsActive,Remark")] BasicDataItem input, bool popup = false)
    {
        if (id != input.Id)
        {
            return NotFound();
        }

        await ValidateHierarchyAsync(input, input.Id);
        if (!ModelState.IsValid)
        {
            ViewBag.IsPopup = popup;
            await PopulateEditorSelectDataAsync(input.TypeId, input.ParentId, input.Id);
            return View(input);
        }

        var updated = await _repository.UpdateItemAsync(input, User?.Identity?.Name);
        if (!updated)
        {
            return NotFound();
        }

        TempData["BasicDataUpdatedTypeCode"] = await ResolveTypeCodeAsync(input.TypeId);
        TempData["BasicDataUpdatedSelectedId"] = input.Id;
        return RedirectToAction(nameof(Index), new { typeId = input.TypeId, popup });
    }

    public async Task<IActionResult> Delete(int? id, bool popup = false)
    {
        if (id == null)
        {
            return NotFound();
        }

        var item = await _repository.GetItemByIdAsync(id.Value);
        if (item == null)
        {
            return NotFound();
        }

        ViewBag.IsPopup = popup;
        return View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, bool popup = false)
    {
        var item = await _repository.GetItemByIdAsync(id);
        if (item == null)
        {
            return RedirectToAction(nameof(Index), new { popup });
        }

        // 行政区或同类层级存在下级节点时，禁止直接删除上级。
        var hasChildren = (await _repository.GetItemsAsync()).Any(x => x.ParentId == id);
        if (hasChildren)
        {
            TempData["BasicDataMessage"] = "存在下级选项，不能删除当前数据。请先调整或删除下级数据。";
            return RedirectToAction(nameof(Index), new { typeId = item.TypeId, popup });
        }

        await _repository.DeleteItemAsync(id, User?.Identity?.Name);
        TempData["BasicDataUpdatedTypeCode"] = await ResolveTypeCodeAsync(item.TypeId);
        TempData["BasicDataUpdatedSelectedId"] = 0;
        return RedirectToAction(nameof(Index), new { typeId = item.TypeId, popup });
    }

    /// <summary>
    /// 获取父级下拉选项接口（用于创建/编辑页按类型动态联动）。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ParentOptions(int typeId, int? excludeId = null)
    {
        var context = await ResolveParentRuleContextAsync(typeId);
        if (context == null)
        {
            return Json(new
            {
                parentLabel = "上级选项",
                parentPlaceholder = "请先选择有效类型",
                parentHint = "未找到当前类型",
                parentRequired = false,
                parentSelectDisabled = true,
                options = Array.Empty<object>()
            });
        }

        var parentItems = await GetAvailableParentItemsAsync(context, excludeId);
        return Json(new
        {
            parentLabel = context.ParentLabel,
            parentPlaceholder = context.ParentPlaceholder,
            parentHint = context.ParentHint,
            parentRequired = context.ParentRequired,
            parentSelectDisabled = context.ParentSelectDisabled,
            options = parentItems.Select(x => new { value = x.Id, text = x.ItemName })
        });
    }

    /// <summary>
    /// 绑定编辑页下拉数据（类型和父级联动）。
    /// </summary>
    private async Task PopulateEditorSelectDataAsync(int? selectedTypeId, int? selectedParentId, int? excludeId = null)
    {
        await PopulateTypeSelectListAsync(selectedTypeId);
        await PopulateParentSelectListAsync(selectedTypeId, selectedParentId, excludeId);
    }

    /// <summary>
    /// 绑定类型下拉选项（来自基础数据类型字典）。
    /// </summary>
    private async Task PopulateTypeSelectListAsync(int? selectedTypeId)
    {
        var types = await _repository.GetTypesAsync();
        foreach (var type in types)
        {
            type.TypeName = NormalizeTypeDisplayName(type.TypeCode, type.TypeName);
        }

        ViewData["TypeId"] = new SelectList(types, "Id", "TypeName", selectedTypeId);
    }

    /// <summary>
    /// 根据类型编码解析类型ID（供员工详情页快捷入口直接按类型编码跳转）。
    /// </summary>
    private async Task<int?> ResolveTypeIdAsync(int? typeId, string? typeCode)
    {
        if (typeId.HasValue)
        {
            return typeId;
        }

        if (string.IsNullOrWhiteSpace(typeCode))
        {
            return null;
        }

        var normalizedTypeCode = typeCode.Trim().ToUpperInvariant();
        if (normalizedTypeCode is not (PositionTypeCode or EthnicityTypeCode or ProfessionalTitleTypeCode or RegionTypeCode or CountryRegionTypeCode or CityTypeCode or CountyTypeCode))
        {
            var type = await _repository.GetTypeByCodeAsync(normalizedTypeCode);
            return type?.Id;
        }

        return (await _repository.GetTypeByCodeAsync(normalizedTypeCode))?.Id;
    }

    /// <summary>
    /// 根据类型ID回查类型编码（用于弹窗保存后通知员工页刷新对应下拉）。
    /// </summary>
    private async Task<string?> ResolveTypeCodeAsync(int typeId)
        => (await _repository.GetTypeByIdAsync(typeId))?.TypeCode;

    /// <summary>
    /// 归一化基础数据类型展示名称（POSITION 统一显示为“职务数据”）。
    /// </summary>
    private static string NormalizeTypeDisplayName(string? typeCode, string? typeName)
    {
        if (string.Equals(typeCode, PositionTypeCode, StringComparison.OrdinalIgnoreCase))
        {
            return "职务数据";
        }

        return typeName ?? string.Empty;
    }

    /// <summary>
    /// 绑定上级选项下拉（按国家地区-城市-县域层级规则动态切换）。
    /// </summary>
    private async Task PopulateParentSelectListAsync(int? typeId, int? selectedParentId, int? excludeId = null)
    {
        if (!typeId.HasValue)
        {
            ViewData["ParentId"] = new SelectList(Array.Empty<BasicDataItem>(), "Id", "ItemName");
            ViewData["ParentLabel"] = "上级选项";
            ViewData["ParentPlaceholder"] = "请先选择类型";
            ViewData["ParentHint"] = "普通类型默认支持同类型层级。";
            ViewData["ParentRequired"] = false;
            ViewData["ParentSelectDisabled"] = true;
            return;
        }

        var context = await ResolveParentRuleContextAsync(typeId.Value);
        if (context == null)
        {
            ViewData["ParentId"] = new SelectList(Array.Empty<BasicDataItem>(), "Id", "ItemName");
            ViewData["ParentLabel"] = "上级选项";
            ViewData["ParentPlaceholder"] = "未找到当前类型";
            ViewData["ParentHint"] = "请检查基础数据类型配置。";
            ViewData["ParentRequired"] = false;
            ViewData["ParentSelectDisabled"] = true;
            return;
        }

        var items = await GetAvailableParentItemsAsync(context, excludeId);
        ViewData["ParentId"] = new SelectList(items, "Id", "ItemName", selectedParentId);
        ViewData["ParentLabel"] = context.ParentLabel;
        ViewData["ParentPlaceholder"] = context.ParentPlaceholder;
        ViewData["ParentHint"] = context.ParentHint;
        ViewData["ParentRequired"] = context.ParentRequired;
        ViewData["ParentSelectDisabled"] = context.ParentSelectDisabled;
    }

    /// <summary>
    /// 校验父级关系（国家地区-城市-县域三层强约束，其他类型默认同类型可选父级）。
    /// </summary>
    private async Task ValidateHierarchyAsync(BasicDataItem input, int? currentItemId = null)
    {
        var context = await ResolveParentRuleContextAsync(input.TypeId);
        if (context == null)
        {
            ModelState.AddModelError(nameof(input.TypeId), "未找到所选基础数据类型。");
            return;
        }

        if (currentItemId.HasValue && input.ParentId == currentItemId.Value)
        {
            ModelState.AddModelError(nameof(input.ParentId), "上级选项不能选择自身。");
            return;
        }

        if (!context.ParentTypeId.HasValue)
        {
            if (context.ParentRequired)
            {
                ModelState.AddModelError(nameof(input.TypeId), $"未找到{context.ParentLabel}对应的基础数据类型，请先维护类型。");
                return;
            }

            if (input.ParentId.HasValue)
            {
                ModelState.AddModelError(nameof(input.ParentId), $"{context.CurrentTypeName}属于一级数据，不能设置上级。");
            }

            return;
        }

        if (!input.ParentId.HasValue)
        {
            if (context.ParentRequired)
            {
                ModelState.AddModelError(nameof(input.ParentId), $"当前类型必须选择{context.ParentLabel}。");
            }

            return;
        }

        var parentItem = await _repository.GetItemByIdAsync(input.ParentId.Value);
        if (parentItem == null)
        {
            ModelState.AddModelError(nameof(input.ParentId), "所选上级选项不存在或已删除。");
            return;
        }

        if (parentItem.TypeId != context.ParentTypeId.Value)
        {
            ModelState.AddModelError(nameof(input.ParentId), $"{context.CurrentTypeName}的上级必须是“{context.ParentTypeName}”类型。");
        }
    }

    /// <summary>
    /// 按当前类型解析父级规则上下文（含父级类型与页面展示提示）。
    /// </summary>
    private async Task<ParentRuleContext?> ResolveParentRuleContextAsync(int typeId)
    {
        var currentType = await _repository.GetTypeByIdAsync(typeId);
        if (currentType == null)
        {
            return null;
        }

        var rule = BuildParentRuleForType(currentType.TypeCode);
        if (string.IsNullOrWhiteSpace(rule.ParentTypeCode))
        {
            return new ParentRuleContext(
                CurrentTypeName: currentType.TypeName,
                ParentTypeName: null,
                ParentTypeId: null,
                ParentRequired: rule.ParentRequired,
                ParentLabel: rule.ParentLabel,
                ParentPlaceholder: rule.ParentPlaceholder,
                ParentHint: rule.ParentHint,
                ParentSelectDisabled: true);
        }

        var parentType = await _repository.GetTypeByCodeAsync(rule.ParentTypeCode);
        if (parentType == null)
        {
            return new ParentRuleContext(
                CurrentTypeName: currentType.TypeName,
                ParentTypeName: rule.ParentTypeCode,
                ParentTypeId: null,
                ParentRequired: rule.ParentRequired,
                ParentLabel: rule.ParentLabel,
                ParentPlaceholder: "缺少上级类型配置",
                ParentHint: $"系统未找到上级类型：{rule.ParentTypeCode}",
                ParentSelectDisabled: true);
        }

        return new ParentRuleContext(
            CurrentTypeName: currentType.TypeName,
            ParentTypeName: parentType.TypeName,
            ParentTypeId: parentType.Id,
            ParentRequired: rule.ParentRequired,
            ParentLabel: rule.ParentLabel,
            ParentPlaceholder: rule.ParentPlaceholder,
            ParentHint: rule.ParentHint,
            ParentSelectDisabled: false);
    }

    /// <summary>
    /// 解析可选父级数据（按规则选择父级类型，并在同类型场景排除当前节点）。
    /// </summary>
    private async Task<List<BasicDataItem>> GetAvailableParentItemsAsync(ParentRuleContext context, int? excludeId)
    {
        if (!context.ParentTypeId.HasValue)
        {
            return [];
        }

        var items = await _repository.GetItemsByTypeIdAsync(context.ParentTypeId.Value);
        if (excludeId.HasValue)
        {
            items = items.Where(x => x.Id != excludeId.Value).ToList();
        }

        return items;
    }

    /// <summary>
    /// 根据类型编码构建父级规则（行政区三层强约束，其它类型默认同类型可选）。
    /// </summary>
    private static ParentRuleDefinition BuildParentRuleForType(string typeCode)
    {
        if (LocationHierarchyRules.TryGetValue(typeCode, out var fixedRule))
        {
            return fixedRule;
        }

        return new ParentRuleDefinition(
            ParentTypeCode: typeCode,
            ParentRequired: false,
            ParentLabel: "上级选项",
            ParentPlaceholder: "无（可选）",
            ParentHint: "普通基础数据默认可选同类型上级。");
    }

    /// <summary>
    /// 父级规则定义（描述当前类型对应的上级类型与页面提示）。
    /// </summary>
    private sealed record ParentRuleDefinition(
        string? ParentTypeCode,
        bool ParentRequired,
        string ParentLabel,
        string ParentPlaceholder,
        string ParentHint);

    /// <summary>
    /// 父级规则解析结果（供校验与页面渲染共用）。
    /// </summary>
    private sealed record ParentRuleContext(
        string CurrentTypeName,
        string? ParentTypeName,
        int? ParentTypeId,
        bool ParentRequired,
        string ParentLabel,
        string ParentPlaceholder,
        string ParentHint,
        bool ParentSelectDisabled);
}

