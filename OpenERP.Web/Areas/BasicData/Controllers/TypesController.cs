using Microsoft.AspNetCore.Mvc;
using OpenERP.BasicData.Data;
using OpenERP.BasicData.Models;

namespace OpenERP.Web.Areas.BasicData.Controllers;

/// <summary>
/// 基础数据类型管理控制器（维护基础数据类型字典）。
/// </summary>
[Area("BasicData")]
[Microsoft.AspNetCore.Authorization.Authorize]
    public class TypesController : Controller
{
    /// <summary>
    /// 基础数据仓储（负责类型和选项数据读写）。
    /// </summary>
    private readonly IBasicDataRepository _repository;

    public TypesController(IBasicDataRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _repository.GetTypesAsync();
        return View(items);
    }

    public IActionResult Create()
    {
        return View(new BasicDataType());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("TypeCode,TypeName,Description")] BasicDataType input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        await _repository.CreateTypeAsync(input, User?.Identity?.Name);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var item = await _repository.GetTypeByIdAsync(id.Value);
        if (item == null)
        {
            return NotFound();
        }

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,TypeCode,TypeName,Description")] BasicDataType input)
    {
        if (id != input.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var updated = await _repository.UpdateTypeAsync(input, User?.Identity?.Name);
        if (!updated)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var item = await _repository.GetTypeByIdAsync(id.Value);
        if (item == null)
        {
            return NotFound();
        }

        return View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _repository.DeleteTypeAsync(id, User?.Identity?.Name);
        return RedirectToAction(nameof(Index));
    }
}

