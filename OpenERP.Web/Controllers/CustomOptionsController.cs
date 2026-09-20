using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using OpenERP.BasicData.Data;
using OpenERP.BasicData.Models;
using OpenERP.HR.Models.Entities;
using OpenERP.Web.Data.HR;
using OpenERP.Web.Models.CustomOptions;

namespace OpenERP.Web.Controllers;

/// <summary>
/// 通用自定义选项维护控制器（为职务、民族、职称等用户可维护下拉数据提供共用页面）。
/// </summary>
public class CustomOptionsController : Controller
{
    /// <summary>
    /// 职务来源键（对应 POSITION 基础数据字典）。
    /// </summary>
    private const string PositionSourceKey = "position";

    /// <summary>
    /// 民族来源键（对应基础数据字典 ETHNICITY）。
    /// </summary>
    private const string EthnicitySourceKey = "ethnicity";

    /// <summary>
    /// 职称来源键（对应基础数据字典 PROFESSIONAL_TITLE）。
    /// </summary>
    private const string ProfessionalTitleSourceKey = "professional-title";

    /// <summary>
    /// 津贴待遇来源键（对应基础数据字典 ALLOWANCE_PACKAGE）。
    /// </summary>
    private const string AllowancePackageSourceKey = "allowance-package";

    /// <summary>
    /// 排班组别来源键（对应基础数据字典 SCHEDULING_GROUP）。
    /// </summary>
    private const string SchedulingGroupSourceKey = "scheduling-group";

    /// <summary>
    /// 银行来源键（对应基础数据字典 BANK）。
    /// </summary>
    private const string BankSourceKey = "bank";

    /// <summary>
    /// 职务基础数据类型编码（对应基础数据字典 POSITION）。
    /// </summary>
    private const string PositionTypeCode = "POSITION";

    /// <summary>
    /// 民族基础数据类型编码（来自基础数据字典）。
    /// </summary>
    private const string EthnicityTypeCode = "ETHNICITY";

    /// <summary>
    /// 职称基础数据类型编码（来自基础数据字典）。
    /// </summary>
    private const string ProfessionalTitleTypeCode = "PROFESSIONAL_TITLE";

    /// <summary>
    /// 津贴待遇基础数据类型编码（对应基础数据字典）。
    /// </summary>
    private const string AllowancePackageTypeCode = "ALLOWANCE_PACKAGE";

    /// <summary>
    /// 排班组别基础数据类型编码（对应基础数据字典）。
    /// </summary>
    private const string SchedulingGroupTypeCode = "SCHEDULING_GROUP";

    /// <summary>
    /// 银行基础数据类型编码（对应基础数据字典）。
    /// </summary>
    private const string BankTypeCode = "BANK";

    /// <summary>
    /// 人资仓储（负责职务与员工引用关系读取）。
    /// </summary>
    private readonly IHrRepository _hrRepository;

    /// <summary>
    /// 基础数据仓储（负责民族、职称等字典数据读写）。
    /// </summary>
    private readonly IBasicDataRepository _basicDataRepository;

    public CustomOptionsController(IHrRepository hrRepository, IBasicDataRepository basicDataRepository)
    {
        _hrRepository = hrRepository;
        _basicDataRepository = basicDataRepository;
    }

    /// <summary>
    /// 共用选项维护主页（列表与编辑器同页展示）。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string source, int? editId = null, bool popup = false)
    {
        var definition = await ResolveSourceDefinitionAsync(source);
        if (definition == null)
        {
            return NotFound();
        }

        var viewModel = await BuildPageViewModelAsync(definition, editId);
        ViewBag.IsPopup = popup;
        return View(viewModel);
    }

    /// <summary>
    /// 保存通用选项数据（同一入口处理新增和修改）。
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([Bind(Prefix = "Editor")] CustomOptionEditorInput input, bool popup = false)
    {
        var definition = await ResolveSourceDefinitionAsync(input.SourceKey);
        if (definition == null)
        {
            return NotFound();
        }

        await ValidateEditorAsync(definition, input);
        if (!ModelState.IsValid)
        {
            var invalidViewModel = await BuildPageViewModelAsync(definition, input);
            ViewBag.IsPopup = popup;
            return View("Index", invalidViewModel);
        }

        var operatorName = User?.Identity?.Name;
        int selectedId;

        if (string.Equals(definition.SourceKey, PositionSourceKey, StringComparison.OrdinalIgnoreCase)
            && definition.BasicDataType == null)
        {
            selectedId = await SavePositionAsync(input, operatorName);
        }
        else
        {
            selectedId = await SaveBasicDataItemAsync(definition, input, operatorName);
        }

        TempData["CustomOptionMessage"] = input.Id > 0
            ? $"{definition.SourceName}已更新。"
            : $"{definition.SourceName}已新增。";
        TempData["CustomOptionUpdatedSource"] = definition.SourceKey;
        TempData["CustomOptionUpdatedSelectedId"] = selectedId;

        return RedirectToAction(nameof(Index), new { source = definition.SourceKey, editId = selectedId, popup });
    }

    /// <summary>
    /// 删除通用选项数据（删除前校验是否仍被员工资料引用）。
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string source, int id, bool popup = false)
    {
        var definition = await ResolveSourceDefinitionAsync(source);
        if (definition == null)
        {
            return NotFound();
        }

        if (await IsOptionInUseAsync(definition.SourceKey, id))
        {
            TempData["CustomOptionMessage"] = $"当前{definition.SourceName}已被员工资料引用，暂不能删除。";
            return RedirectToAction(nameof(Index), new { source = definition.SourceKey, editId = id, popup });
        }

        if (string.Equals(definition.SourceKey, PositionSourceKey, StringComparison.OrdinalIgnoreCase))
        {
            await _hrRepository.DeletePositionAsync(id);
        }
        else
        {
            await _basicDataRepository.DeleteItemAsync(id, User?.Identity?.Name);
        }

        TempData["CustomOptionMessage"] = $"{definition.SourceName}已删除。";
        TempData["CustomOptionUpdatedSource"] = definition.SourceKey;
        TempData["CustomOptionUpdatedSelectedId"] = 0;

        return RedirectToAction(nameof(Index), new { source = definition.SourceKey, popup });
    }

    /// <summary>
    /// 返回通用选项下拉数据（供员工详情页在弹窗保存后刷新下拉框）。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Options(string source)
    {
        var definition = await ResolveSourceDefinitionAsync(source);
        if (definition == null)
        {
            return NotFound();
        }

        var items = await LoadItemsAsync(definition);
        return Json(items.Select(item => new
        {
            value = item.Id,
            text = item.Name
        }));
    }

    /// <summary>
    /// 按来源键解析通用选项来源配置（定义数据来源与页面展示能力）。
    /// </summary>
    private async Task<CustomOptionSourceDefinition?> ResolveSourceDefinitionAsync(string? sourceKey)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
        {
            return null;
        }

        if (string.Equals(sourceKey, PositionSourceKey, StringComparison.OrdinalIgnoreCase))
        {
            var type = await _basicDataRepository.GetTypeByCodeAsync(PositionTypeCode);
            if (type == null)
            {
                return null;
            }

            return new CustomOptionSourceDefinition(
                SourceKey: PositionSourceKey,
                SourceName: "职务",
                Title: "职务选项维护",
                DescriptionLabel: "备注",
                SupportsCode: true,
                SupportsSortOrder: true,
                SupportsIsActive: true,
                BasicDataType: type);
        }

        if (string.Equals(sourceKey, EthnicitySourceKey, StringComparison.OrdinalIgnoreCase))
        {
            var type = await _basicDataRepository.GetTypeByCodeAsync(EthnicityTypeCode);
            if (type == null)
            {
                return null;
            }

            return new CustomOptionSourceDefinition(
                SourceKey: EthnicitySourceKey,
                SourceName: "民族",
                Title: "民族选项维护",
                DescriptionLabel: "备注",
                SupportsCode: true,
                SupportsSortOrder: true,
                SupportsIsActive: true,
                BasicDataType: type);
        }

        if (string.Equals(sourceKey, ProfessionalTitleSourceKey, StringComparison.OrdinalIgnoreCase))
        {
            var type = await _basicDataRepository.GetTypeByCodeAsync(ProfessionalTitleTypeCode);
            if (type == null)
            {
                return null;
            }

            return new CustomOptionSourceDefinition(
                SourceKey: ProfessionalTitleSourceKey,
                SourceName: "职称",
                Title: "职称选项维护",
                DescriptionLabel: "备注",
                SupportsCode: true,
                SupportsSortOrder: true,
                SupportsIsActive: true,
                BasicDataType: type);
        }

        if (string.Equals(sourceKey, AllowancePackageSourceKey, StringComparison.OrdinalIgnoreCase))
        {
            var type = await _basicDataRepository.GetTypeByCodeAsync(AllowancePackageTypeCode);
            if (type == null)
            {
                return null;
            }

            return new CustomOptionSourceDefinition(
                SourceKey: AllowancePackageSourceKey,
                SourceName: "津贴待遇",
                Title: "津贴待遇选项维护",
                DescriptionLabel: "备注",
                SupportsCode: true,
                SupportsSortOrder: true,
                SupportsIsActive: true,
                BasicDataType: type);
        }

        if (string.Equals(sourceKey, SchedulingGroupSourceKey, StringComparison.OrdinalIgnoreCase))
        {
            var type = await _basicDataRepository.GetTypeByCodeAsync(SchedulingGroupTypeCode);
            if (type == null)
            {
                return null;
            }

            return new CustomOptionSourceDefinition(
                SourceKey: SchedulingGroupSourceKey,
                SourceName: "排班组别",
                Title: "排班组别选项维护",
                DescriptionLabel: "备注",
                SupportsCode: true,
                SupportsSortOrder: true,
                SupportsIsActive: true,
                BasicDataType: type);
        }

        if (string.Equals(sourceKey, BankSourceKey, StringComparison.OrdinalIgnoreCase))
        {
            var type = await _basicDataRepository.GetTypeByCodeAsync(BankTypeCode);
            if (type == null)
            {
                return null;
            }

            return new CustomOptionSourceDefinition(
                SourceKey: BankSourceKey,
                SourceName: "银行",
                Title: "银行选项维护",
                DescriptionLabel: "备注",
                SupportsCode: true,
                SupportsSortOrder: true,
                SupportsIsActive: true,
                BasicDataType: type);
        }

        return null;
    }

    /// <summary>
    /// 构建共用维护页视图模型（列表与编辑器统一封装）。
    /// </summary>
    private async Task<CustomOptionManagementPageViewModel> BuildPageViewModelAsync(
        CustomOptionSourceDefinition definition,
        int? editId)
    {
        CustomOptionEditorInput editor;

        if (editId.HasValue && editId.Value > 0)
        {
            editor = await LoadEditorAsync(definition, editId.Value) ?? CreateDefaultEditor(definition);
        }
        else
        {
            editor = CreateDefaultEditor(definition);
        }

        return await BuildPageViewModelAsync(definition, editor);
    }

    /// <summary>
    /// 构建共用维护页视图模型（保留当前编辑器输入，供校验失败时回显）。
    /// </summary>
    private async Task<CustomOptionManagementPageViewModel> BuildPageViewModelAsync(
        CustomOptionSourceDefinition definition,
        CustomOptionEditorInput editor)
    {
        return new CustomOptionManagementPageViewModel
        {
            SourceKey = definition.SourceKey,
            Title = definition.Title,
            SourceName = definition.SourceName,
            DescriptionLabel = definition.DescriptionLabel,
            SupportsCode = definition.SupportsCode,
            SupportsSortOrder = definition.SupportsSortOrder,
            SupportsIsActive = definition.SupportsIsActive,
            Items = await LoadItemsAsync(definition),
            Editor = editor
        };
    }

    /// <summary>
    /// 加载共用选项列表数据（统一映射职位和基础数据项）。
    /// </summary>
    private async Task<IReadOnlyList<CustomOptionListItemViewModel>> LoadItemsAsync(CustomOptionSourceDefinition definition)
    {
        if (string.Equals(definition.SourceKey, PositionSourceKey, StringComparison.OrdinalIgnoreCase)
            && definition.BasicDataType == null)
        {
            var positions = await _hrRepository.GetPositionsAsync();
            return positions
                .OrderBy(position => position.Name)
                .Select(position => new CustomOptionListItemViewModel
                {
                    Id = position.Id,
                    Name = position.Name,
                    Description = position.Description
                })
                .ToList();
        }

        var basicDataItems = await _basicDataRepository.GetItemsByTypeIdAsync(definition.BasicDataType!.Id);
        return basicDataItems
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.ItemName)
            .Select(item => new CustomOptionListItemViewModel
            {
                Id = item.Id,
                Code = item.ItemCode,
                Name = item.ItemName,
                SortOrder = item.SortOrder,
                IsActive = item.IsActive,
                Description = item.Remark
            })
            .ToList();
    }

    /// <summary>
    /// 加载编辑器数据（根据来源键读取职位或基础数据项详情）。
    /// </summary>
    private async Task<CustomOptionEditorInput?> LoadEditorAsync(CustomOptionSourceDefinition definition, int id)
    {
        if (string.Equals(definition.SourceKey, PositionSourceKey, StringComparison.OrdinalIgnoreCase)
            && definition.BasicDataType == null)
        {
            var position = await _hrRepository.GetPositionByIdAsync(id);
            if (position == null)
            {
                return null;
            }

            return new CustomOptionEditorInput
            {
                Id = position.Id,
                SourceKey = definition.SourceKey,
                Name = position.Name,
                Description = position.Description
            };
        }

        var item = await _basicDataRepository.GetItemByIdAsync(id);
        if (item == null)
        {
            return null;
        }

        return new CustomOptionEditorInput
        {
            Id = item.Id,
            SourceKey = definition.SourceKey,
            Code = item.ItemCode,
            Name = item.ItemName,
            SortOrder = item.SortOrder,
            IsActive = item.IsActive,
            Description = item.Remark
        };
    }

    /// <summary>
    /// 创建默认编辑器数据（用于新增状态）。
    /// </summary>
    private static CustomOptionEditorInput CreateDefaultEditor(CustomOptionSourceDefinition definition)
    {
        return new CustomOptionEditorInput
        {
            SourceKey = definition.SourceKey,
            SortOrder = 0,
            IsActive = true
        };
    }

    /// <summary>
    /// 校验编辑器输入（按不同来源执行差异化规则）。
    /// </summary>
    private async Task ValidateEditorAsync(CustomOptionSourceDefinition definition, CustomOptionEditorInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            ModelState.AddModelError(nameof(input.Name), $"{definition.SourceName}名称不能为空。");
        }

        if (!TryValidateModel(input))
        {
            return;
        }

        if (definition.SupportsCode && string.IsNullOrWhiteSpace(input.Code))
        {
            ModelState.AddModelError(nameof(input.Code), $"{definition.SourceName}编码不能为空。");
        }

        if (string.Equals(definition.SourceKey, PositionSourceKey, StringComparison.OrdinalIgnoreCase)
            && definition.BasicDataType == null)
        {
            var existingPositions = await _hrRepository.GetPositionsAsync();
            if (existingPositions.Any(position =>
                    position.Id != input.Id &&
                    string.Equals(position.Name, input.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(input.Name), "已存在同名职务，请调整后再保存。");
            }

            return;
        }

        var existingItems = await _basicDataRepository.GetItemsByTypeIdAsync(definition.BasicDataType!.Id);
        if (existingItems.Any(item =>
                item.Id != input.Id &&
                string.Equals(item.ItemCode, input.Code?.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(input.Code), "当前编码已存在，请调整后再保存。");
        }
    }

    /// <summary>
    /// 保存职位数据（新增时返回新职位ID，修改时返回当前ID）。
    /// </summary>
    private async Task<int> SavePositionAsync(CustomOptionEditorInput input, string? operatorName)
    {
        if (input.Id > 0)
        {
            var updated = new Position
            {
                Id = input.Id,
                Name = input.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
                UpdatedBy = operatorName
            };

            await _hrRepository.UpdatePositionAsync(updated);
            return input.Id;
        }

        var created = new Position
        {
            Name = input.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            CreatedBy = operatorName
        };

        return await _hrRepository.CreatePositionAsync(created);
    }

    /// <summary>
    /// 保存基础数据项（新增时按编码回查新记录ID，修改时返回当前ID）。
    /// </summary>
    private async Task<int> SaveBasicDataItemAsync(
        CustomOptionSourceDefinition definition,
        CustomOptionEditorInput input,
        string? operatorName)
    {
        var item = new BasicDataItem
        {
            Id = input.Id,
            TypeId = definition.BasicDataType!.Id,
            ItemCode = input.Code!.Trim(),
            ItemName = input.Name.Trim(),
            SortOrder = input.SortOrder,
            IsActive = input.IsActive,
            Remark = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim()
        };

        if (input.Id > 0)
        {
            await _basicDataRepository.UpdateItemAsync(item, operatorName);
            return input.Id;
        }

        await _basicDataRepository.CreateItemAsync(item, operatorName);
        var createdItem = (await _basicDataRepository.GetItemsByTypeIdAsync(definition.BasicDataType.Id))
            .FirstOrDefault(existingItem =>
                string.Equals(existingItem.ItemCode, item.ItemCode, StringComparison.OrdinalIgnoreCase));

        return createdItem?.Id ?? 0;
    }

    /// <summary>
    /// 校验当前选项是否已被员工资料引用（避免删除后出现悬空下拉值）。
    /// </summary>
    private async Task<bool> IsOptionInUseAsync(string sourceKey, int optionId)
    {
        var employees = await _hrRepository.GetEmployeesAsync();

        return sourceKey switch
        {
            PositionSourceKey => employees.Any(employee => employee.PositionId == optionId),
            EthnicitySourceKey => employees.Any(employee => employee.EthnicityId == optionId),
            ProfessionalTitleSourceKey => employees.Any(employee => employee.ProfessionalTitleId == optionId),
            AllowancePackageSourceKey => employees.Any(employee => employee.AllowancePackageId == optionId),
            SchedulingGroupSourceKey => employees.Any(employee => employee.SchedulingGroupId == optionId),
            BankSourceKey => employees.Any(employee => employee.BankId == optionId),
            _ => false
        };
    }

    /// <summary>
    /// 通用选项来源配置（定义页面字段能力和基础数据类型上下文）。
    /// </summary>
    private sealed record CustomOptionSourceDefinition(
        string SourceKey,
        string SourceName,
        string Title,
        string DescriptionLabel,
        bool SupportsCode,
        bool SupportsSortOrder,
        bool SupportsIsActive,
        BasicDataType? BasicDataType);
}
