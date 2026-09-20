using Microsoft.AspNetCore.Mvc;
using OpenERP.BasicData.Data;
using OpenERP.Web.Areas.BasicData.Models;

namespace OpenERP.Web.Areas.BasicData.Controllers;

/// <summary>
/// 基础数据模块首页控制器（统一承载常用基础数据项目入口）。
/// </summary>
[Area("BasicData")]
[Microsoft.AspNetCore.Authorization.Authorize]
    public class HomeController : Controller
{
    /// <summary>
    /// 基础数据仓储（负责类型和选项数据读写）。
    /// </summary>
    private readonly IBasicDataRepository _repository;

    /// <summary>
    /// 基础数据模块首页固定入口配置（按业务常用程度排序）。
    /// </summary>
    private static readonly (string TypeCode, string DisplayName, string Description, string IconCss)[] ModuleEntries =
    [
        ("POSITION", "职务数据", "维护职务编码、职务名称等基础数据", "bi bi-person-workspace"),
        ("EMPLOYMENT_STATUS", "在职状态数据", "维护在职、试用、离职等员工状态字典", "bi bi-person-check"),
        ("ETHNICITY", "民族数据", "维护员工民族基础字典", "bi bi-people"),
        ("PROFESSIONAL_TITLE", "职称数据", "维护专业职称基础字典", "bi bi-patch-check"),
        ("COUNTRY_REGION", "国家地区数据", "一级：维护国家和地区基础字典", "bi bi-globe2"),
        ("CITY", "城市数据", "二级：城市上级必须为国家地区", "bi bi-building"),
        ("COUNTY", "县域数据", "三级：县域上级必须为城市", "bi bi-geo-alt")
    ];

    public HomeController(IBasicDataRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 基础数据模块首页（展示常用基础数据项目入口）。
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var types = await _repository.GetTypesAsync();
        // 按类型编码构建映射，支持模块入口快速定位到对应类型ID。
        var typeMap = types.ToDictionary(x => x.TypeCode, StringComparer.OrdinalIgnoreCase);

        // 组装首页入口视图数据。
        var entries = ModuleEntries
            .Select(x => new BasicDataModuleEntry
            {
                TypeCode = x.TypeCode,
                DisplayName = x.DisplayName,
                Description = x.Description,
                IconCss = x.IconCss,
                TypeId = typeMap.TryGetValue(x.TypeCode, out var type) ? type.Id : null
            })
            .ToList();

        ViewData["TotalTypeCount"] = types.Count;
        return View(entries);
    }

    /// <summary>
    /// 按类型编码打开对应的基础数据选项管理页。
    /// </summary>
    public async Task<IActionResult> Open(string typeCode)
    {
        if (string.IsNullOrWhiteSpace(typeCode))
        {
            return RedirectToAction(nameof(Index));
        }

        var type = await _repository.GetTypeByCodeAsync(typeCode);
        if (type == null)
        {
            TempData["BasicDataMessage"] = $"未找到类型编码：{typeCode}";
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction("Index", "Items", new { area = "BasicData", typeId = type.Id });
    }
}

