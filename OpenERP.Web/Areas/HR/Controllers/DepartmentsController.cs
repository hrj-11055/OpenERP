/*
 * File: OpenERP.Web/Areas/HR/Controllers/DepartmentsController.cs
 * Description: 部门管理控制器（负责部门列表展示与新增操作）�? */

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OpenERP.HR.Models.Entities;
using OpenERP.Web.Data.HR;

namespace OpenERP.Web.Areas.HR.Controllers
{
    /// <summary>
    /// 部门管理控制器（处理部门增删改查请求）�?    /// </summary>
    [Area("HR")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class DepartmentsController : Controller
    {
        /// <summary>
        /// 人资仓储（读取和写入部门数据）�?        /// </summary>
        private readonly IHrRepository _hrRepository;

        /// <summary>
        /// 初始化部门管理控制器�?        /// </summary>
        public DepartmentsController(IHrRepository hrRepository)
        {
            _hrRepository = hrRepository;
        }

        /// <summary>
        /// 部门列表页（展示所有部门记录）�?        /// </summary>
        public async Task<IActionResult> Index()
        {
            return View(await _hrRepository.GetDepartmentsAsync());
        }

        /// <summary>
        /// 部门新增页（展示新增表单）�?        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// 部门新增提交（保存新部门记录）�?        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Department department)
        {
            if (ModelState.IsValid)
            {
                await _hrRepository.CreateDepartmentAsync(department);
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }
    }
}

