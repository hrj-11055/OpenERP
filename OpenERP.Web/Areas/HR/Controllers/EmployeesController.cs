/*
 * 文件：OpenERP.Web/Areas/HR/Controllers/EmployeesController.cs
 * 说明：员工管理控制器，负责员工资料、员工主图与相关页面请求。
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using OpenERP.BasicData.Data;
using OpenERP.BasicData.Models;
using OpenERP.HR.Models.Entities;
using OpenERP.Web.Data.HR;
using OpenERP.Web.Areas.HR.ViewModels.Employees;
using OpenERP.Web.Documents;
using OpenERP.Web.Localization;

namespace OpenERP.Web.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize]
    public class EmployeesController : Controller
    {
        /// <summary>
        /// 人资仓储（读取员工、组织、部门与职位数据）。
        /// </summary>
        private readonly IHrRepository _hrRepository;

        /// <summary>
        /// 基础数据仓储（读取民族、职称等用户自定义下拉数据）。
        /// </summary>
        private readonly IBasicDataRepository _basicDataRepository;

        /// <summary>
        /// 共享本地化资源（用于员工页面与表单国际化）。
        /// </summary>
        private readonly IStringLocalizer<SharedResource> _localizer;

        /// <summary>
        /// 通用文档管理服务（用于校验员工图片文档是否属于当前员工）。
        /// </summary>
        private readonly ICommonDocumentService _documentService;

        /// <summary>
        /// 员工图片文档功能编码（对应员工“更多图片”上传的图片集合）。
        /// </summary>
        private const string EmployeePhotoFeatureCode = "HR_EMPLOYEE_PHOTO";

        /// <summary>
        /// 职位基础数据类型编码（对应基础数据字典 POSITION）。
        /// </summary>
        private const string PositionTypeCode = "POSITION";

        /// <summary>
        /// 民族基础数据类型编码（对应基础数据字典 ETHNICITY）。
        /// </summary>
        private const string EthnicityTypeCode = "ETHNICITY";

        /// <summary>
        /// 职称基础数据类型编码（对应基础数据字典 PROFESSIONAL_TITLE）。
        /// </summary>
        private const string ProfessionalTitleTypeCode = "PROFESSIONAL_TITLE";

        /// <summary>
        /// 国家/地区基础数据类型编码（对应员工国家/地区下拉）。
        /// </summary>
        private const string CountryRegionTypeCode = "COUNTRY_REGION";

        /// <summary>
        /// 城市基础数据类型编码（对应员工城市下拉）。
        /// </summary>
        private const string CityTypeCode = "CITY";

        /// <summary>
        /// 县/区基础数据类型编码（对应员工县/区下拉）。
        /// </summary>
        private const string CountyTypeCode = "COUNTY";

        /// <summary>
        /// 津贴待遇基础数据类型编码（对应基础数据字典 ALLOWANCE_PACKAGE）。
        /// </summary>
        private const string AllowancePackageTypeCode = "ALLOWANCE_PACKAGE";

        /// <summary>
        /// 排班组别基础数据类型编码（对应基础数据字典 SCHEDULING_GROUP）。
        /// </summary>
        private const string SchedulingGroupTypeCode = "SCHEDULING_GROUP";

        /// <summary>
        /// 银行基础数据类型编码（对应基础数据字典 BANK）。
        /// </summary>
        private const string BankTypeCode = "BANK";

        public EmployeesController(
            IHrRepository hrRepository,
            IBasicDataRepository basicDataRepository,
            ICommonDocumentService documentService,
            IStringLocalizer<SharedResource> localizer)
        {
            _hrRepository = hrRepository;
            _basicDataRepository = basicDataRepository;
            _documentService = documentService;
            _localizer = localizer;
        }

        /// <summary>
        /// 人员管理列表页（使用页面模型承载展示数据）。
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var employees = await _hrRepository.GetEmployeesAsync();
            var records = employees
                .Select((employee, index) => BuildEmployeeListItem(employee, index + 1))
                .ToList();

            var viewModel = new EmployeeIndexViewModel
            {
                Records = records,
                FilteredCount = records.Count,
                TotalCount = records.Count
            };

            return View(viewModel);
        }

        /// <summary>
        /// 新增员工页面（可传入 copyFromId 从已有员工复制资料）。
        /// </summary>
        public async Task<IActionResult> Create(bool popup = false, int? copyFromId = null)
        {
            Employee employee;

            if (copyFromId.HasValue && copyFromId.Value > 0)
            {
                // 从来源员工复制资料，并清除主键与唯一标识字段。
                var source = await _hrRepository.GetEmployeeByIdAsync(copyFromId.Value);
                if (source != null)
                {
                    employee = new Employee
                    {
                        EmployeeCode = $"{source.EmployeeCode}-COPY",
                        LastName = source.LastName,
                        FirstName = source.FirstName,
                        AliasName = source.AliasName,
                        OrganizationId = source.OrganizationId,
                        DepartmentId = source.DepartmentId,
                        PositionId = source.PositionId,
                        GroupId = source.GroupId,
                        GenderId = source.GenderId,
                        BirthDate = source.BirthDate,
                        IdCardNumber = source.IdCardNumber,
                        EthnicityId = source.EthnicityId,
                        MaritalStatusId = source.MaritalStatusId,
                        EducationLevelId = source.EducationLevelId,
                        EducationCertificateNumber = source.EducationCertificateNumber,
                        ProfessionalTitleId = source.ProfessionalTitleId,
                        PhoneNumber = source.PhoneNumber,
                        PhoneNumber2 = source.PhoneNumber2,
                        PhoneNumber3 = source.PhoneNumber3,
                        CountryRegionId = source.CountryRegionId,
                        CityId = source.CityId,
                        CountyId = source.CountyId,
                        JobTitle = source.JobTitle,
                        Address = source.Address,
                        Email = source.Email,
                        Remarks = source.Remarks,
                        EmergencyContact = source.EmergencyContact,
                        EmergencyContactPhone = source.EmergencyContactPhone,
                        Referrer = source.Referrer,
                        ArchivePath = source.ArchivePath,
                        PhotoPath = source.PhotoPath,
                        HireDate = source.HireDate,
                        LeaveDate = source.LeaveDate,
                        EmploymentTypeId = source.EmploymentTypeId,
                        AllowancePackageId = source.AllowancePackageId,
                        ProbationEndDate = source.ProbationEndDate,
                        AnnualLeaveCalculationMethodId = source.AnnualLeaveCalculationMethodId,
                        IsAttendanceRequired = source.IsAttendanceRequired,
                        CurrentYearAnnualLeaveDays = source.CurrentYearAnnualLeaveDays,
                        AnnualLeaveMaxAccumulatedDays = source.AnnualLeaveMaxAccumulatedDays,
                        AnnualLeaveRemainingDays = source.AnnualLeaveRemainingDays,
                        AnnualLeaveIncrementStartYears = source.AnnualLeaveIncrementStartYears,
                        AnnualLeaveIncrementPerYearDays = source.AnnualLeaveIncrementPerYearDays,
                        AnnualLeaveCapDays = source.AnnualLeaveCapDays,
                        DefaultShiftId = source.DefaultShiftId,
                        SchedulingGroupId = source.SchedulingGroupId,
                        IsAutoSchedulingEnabled = source.IsAutoSchedulingEnabled,
                        SalaryGradeId = source.SalaryGradeId,
                        PayrollCompanyId = source.PayrollCompanyId,
                        BankAccountNumber = source.BankAccountNumber,
                        BankAccountName = source.BankAccountName,
                        BankId = source.BankId
                    };
                }
                else
                {
                    employee = CreateDefaultEmployee();
                }
            }
            else
            {
                employee = CreateDefaultEmployee();
            }

            ViewBag.IsPopup = popup;
            await PopulateSelectListsAsync(employee, applyDefaults: true);
            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("EmployeeCode,FirstName,LastName,OrganizationId,Email,PhoneNumber,PhoneNumber2,PhoneNumber3,JobTitle,DepartmentId,PositionId,GroupId,GenderId,AliasName,BirthDate,IdCardNumber,CardNumber,EthnicityId,MaritalStatusId,EducationLevelId,EducationCertificateNumber,ProfessionalTitleId,CountryRegionId,CityId,CountyId,Address,Remarks,EmergencyContact,EmergencyContactPhone,Referrer,ArchivePath,PhotoPath,LoginAccount,LoginPassword,HireDate,LeaveDate,EmploymentTypeId,AllowancePackageId,ProbationEndDate,AnnualLeaveCalculationMethodId,IsAttendanceRequired,CurrentYearAnnualLeaveDays,AnnualLeaveMaxAccumulatedDays,AnnualLeaveRemainingDays,AnnualLeaveIncrementStartYears,AnnualLeaveIncrementPerYearDays,AnnualLeaveCapDays,DefaultShiftId,SchedulingGroupId,IsAutoSchedulingEnabled,SalaryGradeId,PayrollCompanyId,BankAccountNumber,BankAccountName,BankId")]
            Employee employee,
            bool popup = false)
        {
            await ValidateLocationSelectionAsync(
                employee.CountryRegionId,
                employee.CityId,
                employee.CountyId,
                nameof(Employee.CountryRegionId),
                nameof(Employee.CityId),
                nameof(Employee.CountyId));
            await ValidateEmployeeCardNumberAsync(employee);

            if (ModelState.IsValid)
            {
                employee.CreatedBy = await ResolveCurrentOperatorDisplayNameAsync();
                await _hrRepository.CreateEmployeeAsync(employee);
                return popup ? BuildDetailPageCloseResult() : RedirectToAction(nameof(Index));
            }

            ViewBag.IsPopup = popup;
            await PopulateSelectListsAsync(employee);
            return View(employee);
        }

        public async Task<IActionResult> Edit(int? id, bool popup = false)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _hrRepository.GetEmployeeByIdAsync(id.Value);
            if (employee == null)
            {
                return NotFound();
            }

            ViewBag.IsPopup = popup;
            await PopulateSelectListsAsync(employee);
            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,EmployeeCode,FirstName,LastName,OrganizationId,Email,PhoneNumber,PhoneNumber2,PhoneNumber3,JobTitle,DepartmentId,PositionId,GroupId,GenderId,AliasName,BirthDate,IdCardNumber,CardNumber,EthnicityId,MaritalStatusId,EducationLevelId,EducationCertificateNumber,ProfessionalTitleId,CountryRegionId,CityId,CountyId,Address,Remarks,EmergencyContact,EmergencyContactPhone,Referrer,ArchivePath,PhotoPath,LoginAccount,LoginPassword,HireDate,LeaveDate,EmploymentTypeId,AllowancePackageId,ProbationEndDate,AnnualLeaveCalculationMethodId,IsAttendanceRequired,CurrentYearAnnualLeaveDays,AnnualLeaveMaxAccumulatedDays,AnnualLeaveRemainingDays,AnnualLeaveIncrementStartYears,AnnualLeaveIncrementPerYearDays,AnnualLeaveCapDays,DefaultShiftId,SchedulingGroupId,IsAutoSchedulingEnabled,SalaryGradeId,PayrollCompanyId,BankAccountNumber,BankAccountName,BankId")]
            Employee employee,
            bool popup = false)
        {
            if (id != employee.Id)
            {
                return NotFound();
            }

            var existingEmployee = await _hrRepository.GetEmployeeByIdAsync(id);
            if (existingEmployee == null)
            {
                return NotFound();
            }

            if (!Request.HasFormContentType || !Request.Form.ContainsKey(nameof(Employee.LoginAccount)))
            {
                employee.LoginAccount = existingEmployee.LoginAccount;
            }

            if (!Request.HasFormContentType || !Request.Form.ContainsKey(nameof(Employee.LoginPassword)))
            {
                employee.LoginPassword = existingEmployee.LoginPassword;
            }

            await ValidateLocationSelectionAsync(
                employee.CountryRegionId,
                employee.CityId,
                employee.CountyId,
                nameof(Employee.CountryRegionId),
                nameof(Employee.CityId),
                nameof(Employee.CountyId));
            await ValidateEmployeeCardNumberAsync(employee, id);

            if (ModelState.IsValid)
            {
                employee.UpdatedBy = await ResolveCurrentOperatorDisplayNameAsync();
                var updated = await _hrRepository.UpdateEmployeeAsync(employee);
                if (!updated)
                {
                    return NotFound();
                }

                return popup ? BuildDetailPageCloseResult() : RedirectToAction(nameof(Index));
            }

            ViewBag.IsPopup = popup;
            await PopulateSelectListsAsync(employee);
            return View(employee);
        }

        /// <summary>
        /// 员工详情查看页面（只读模式，需点击编辑才能修改）。
        /// </summary>
        public async Task<IActionResult> Details(int? id, bool popup = false)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _hrRepository.GetEmployeeByIdAsync(id.Value);
            if (employee == null)
            {
                return NotFound();
            }

            ViewBag.IsPopup = popup;
            ViewBag.IsReadOnly = true;
            await PopulateSelectListsAsync(employee);
            return View(employee);
        }

        /// <summary>
        /// 员工删除确认页面（展示员工信息供确认后执行软删除）。
        /// </summary>
        public async Task<IActionResult> Delete(int? id, bool popup = false)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _hrRepository.GetEmployeeDetailsAsync(id.Value);
            if (employee == null)
            {
                return NotFound();
            }

            ViewBag.IsPopup = popup;
            return View(employee);
        }

        /// <summary>
        /// 确认删除员工（标记 IsDeleted 为软删除）。
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, bool popup = false)
        {
            await _hrRepository.DeleteEmployeeAsync(id);
            return popup ? BuildDetailPageCloseResult() : RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 获取员工地址级联下拉选项（按国家/地区或城市过滤下级数据）。
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> LocationOptions(string level, int? parentId = null)
        {
            var normalizedLevel = level?.Trim().ToUpperInvariant();
            var typeCode = normalizedLevel switch
            {
                "COUNTRY_REGION" => CountryRegionTypeCode,
                "CITY" => CityTypeCode,
                "COUNTY" => CountyTypeCode,
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(typeCode))
            {
                return Json(Array.Empty<object>());
            }

            var options = await GetLocationItemsAsync(typeCode, parentId);
            return Json(options.Select(item => new
            {
                value = item.Id,
                text = item.ItemName
            }));
        }

        /// <summary>
        /// 设置员工主图（从员工图片文档集合中选择一张图片写入 PhotoPath）。
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimaryPhoto([FromForm] int employeeId, [FromForm] int? documentId)
        {
            if (employeeId <= 0)
            {
                return BadRequest(new { message = "请先保存员工基本资料，再设置员工主图。" });
            }

            var employee = await _hrRepository.GetEmployeeByIdAsync(employeeId);
            if (employee == null)
            {
                return NotFound(new { message = "未找到员工资料。" });
            }

            string? photoPath = null;
            if (documentId.HasValue && documentId.Value > 0)
            {
                var document = await _documentService.GetDocumentAsync(documentId.Value);
                if (document == null
                    || !string.Equals(document.FeatureCode, EmployeePhotoFeatureCode, StringComparison.OrdinalIgnoreCase)
                    || document.EntityId != employeeId
                    || !IsEmployeePhotoDocument(document))
                {
                    return BadRequest(new { message = "请选择当前员工名下的有效图片文档。" });
                }

                photoPath = $"/api/documents/{document.Id}/open";
            }

            var updatedBy = await ResolveCurrentOperatorDisplayNameAsync();
            var updated = await _hrRepository.UpdateEmployeePhotoPathAsync(employeeId, photoPath, updatedBy);
            if (!updated)
            {
                return NotFound(new { message = "未找到可更新的员工资料。" });
            }

            return Ok(new
            {
                message = string.IsNullOrWhiteSpace(photoPath) ? "员工主图已清除。" : "员工主图已更新。",
                photoPath
            });
        }

        /// <summary>
        /// 构建员工列表项视图模型（负责实体到页面展示字段的转换）。
        /// </summary>
        private EmployeeListItemViewModel BuildEmployeeListItem(Employee employee, int sequenceNo)
        {
            var employeeCode = BuildEmployeeCode(employee, sequenceNo);
            var displayName = BuildDisplayName(employee);
            var employmentStatusName = BuildEmploymentStatus(employee);
            var organizationName = BuildOrganizationName(employee);
            var departmentName = BuildDepartmentLabel(employee);
            var groupName = BuildGroupLabel(employee);
            var jobTitle = BuildJobTitleLabel(employee, sequenceNo);
            var phone = BuildPhoneLabel(employee);
            var fax = BuildFaxLabel(employee);
            var email = BuildEmailLabel(employee);
            var remarks = BuildRemarkLabel(employee);

            return new EmployeeListItemViewModel
            {
                Id = employee.Id,
                SequenceNo = sequenceNo,
                EmployeeCode = employeeCode,
                DisplayName = displayName,
                EmploymentStatusName = employmentStatusName,
                ArchiveLabel = _localizer["Employee_ArchiveText"],
                CardNumber = employee.CardNumber?.Trim() ?? string.Empty,
                GenderName = BuildGenderLabel(employee),
                OrganizationName = organizationName,
                DepartmentName = departmentName,
                GroupName = groupName,
                JobTitle = jobTitle,
                Phone = phone,
                Fax = fax,
                Email = email,
                Remarks = remarks,
                LastModifiedBy = employee.UpdatedBy ?? employee.CreatedBy ?? _localizer["Common_Admin"],
                LastModifiedAtText = (employee.UpdatedAt ?? employee.CreatedAt).ToString("yyyy-MM-dd HH:mm"),
                SearchText = string.Join(
                    "|",
                    new[]
                    {
                        employeeCode,
                        displayName,
                        employmentStatusName,
                        employee.CardNumber ?? string.Empty,
                        organizationName,
                        departmentName,
                        groupName,
                        jobTitle,
                        phone,
                        fax,
                        email,
                        remarks
                    }).ToLowerInvariant()
            };
        }

        /// <summary>
        /// 生成员工展示姓名，中文姓名直接拼接，英文姓名保留空格。
        /// </summary>
        private string BuildDisplayName(Employee employee)
        {
            var firstName = employee.FirstName?.Trim() ?? string.Empty;
            var lastName = employee.LastName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
            {
                return _localizer["Employee_NotNamed"];
            }

            if (ContainsLatinLetter(firstName) || ContainsLatinLetter(lastName))
            {
                return string.Join(" ", new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
            }

            return $"{lastName}{firstName}";
        }

        /// <summary>
        /// 生成员工编号，未维护时按序号补齐显示。
        /// </summary>
        private static string BuildEmployeeCode(Employee employee, int sequenceNo)
            => !string.IsNullOrWhiteSpace(employee.EmployeeCode) ? employee.EmployeeCode : $"A{sequenceNo:000}";

        /// <summary>
        /// 生成员工在职状态文本。
        /// </summary>
        private string BuildEmploymentStatus(Employee employee)
            => employee.IsDeleted
                ? _localizer["Employee_Status_Disabled"]
                : employee.LeaveDate.HasValue
                    ? _localizer["Employee_Status_Left"]
                    : _localizer["Employee_Status_Active"];

        /// <summary>
        /// 生成性别文本，未维护时显示默认值。
        /// </summary>
        private string BuildGenderLabel(Employee employee)
            => employee.GenderId switch
            {
                2 => _localizer["Employee_Gender_Female"],
                _ => _localizer["Employee_Gender_Male"]
            };

        /// <summary>
        /// 生成所属组织名称，优先展示组织简称。
        /// </summary>
        private string BuildOrganizationName(Employee employee)
        {
            var organizationName = employee.Organization?.OrganizationName;

            if (string.IsNullOrWhiteSpace(organizationName))
            {
                return _localizer["Employee_DefaultOrganizationName"];
            }

            foreach (var removableSuffix in new[] { "股份有限公司", "集团有限公司", "有限责任公司", "有限公司" })
            {
                if (organizationName.EndsWith(removableSuffix, StringComparison.Ordinal))
                {
                    return organizationName[..^removableSuffix.Length];
                }
            }

            return organizationName;
        }

        /// <summary>
        /// 生成部门名称，未配置时使用默认值。
        /// </summary>
        private string BuildDepartmentLabel(Employee employee)
            => employee.Department?.Name ?? _localizer["Employee_DefaultDepartment"];

        /// <summary>
        /// 生成组别名称，未配置时显示默认组别。
        /// </summary>
        private string BuildGroupLabel(Employee employee)
            => employee.GroupId.HasValue
                ? _localizer["Employee_GroupPattern", employee.GroupId.Value]
                : _localizer["Employee_DefaultGroup"];

        /// <summary>
        /// 生成职务展示内容，未维护时使用默认职务。
        /// </summary>
        private string BuildJobTitleLabel(Employee employee, int sequenceNo)
            => employee.JobTitle ?? employee.Position?.Name ?? (sequenceNo switch
            {
                1 => _localizer["Employee_JobTitle_Manager"],
                2 => _localizer["Employee_JobTitle_Accountant"],
                3 => _localizer["Employee_JobTitle_Director"],
                _ => _localizer["Employee_JobTitle_Manager"]
            });

        /// <summary>
        /// 生成电话展示内容，未维护时使用演示号码。
        /// </summary>
        private static string BuildPhoneLabel(Employee employee)
            => employee.PhoneNumber ?? "(020) 6666 8888";

        /// <summary>
        /// 生成传真展示内容，未维护时使用电话二或演示号码。
        /// </summary>
        private static string BuildFaxLabel(Employee employee)
            => employee.PhoneNumber2 ?? "(020) 6666 8889";

        /// <summary>
        /// 生成邮箱展示内容，未维护时使用演示邮箱。
        /// </summary>
        private static string BuildEmailLabel(Employee employee)
            => string.IsNullOrWhiteSpace(employee.Email) ? "jeky@honeyi.com" : employee.Email;

        /// <summary>
        /// 生成备注展示内容，未填写时返回空文本。
        /// </summary>
        private string BuildRemarkLabel(Employee employee)
            => string.IsNullOrWhiteSpace(employee.Remarks) ? string.Empty : employee.Remarks;

        /// <summary>
        /// 判断字符串中是否包含英文字母，用于姓名显示格式切换。
        /// </summary>
        private static bool ContainsLatinLetter(string value)
            => value.Any(character => character is >= 'A' and <= 'Z' or >= 'a' and <= 'z');

        /// <summary>
        /// 解析当前登录操作人的显示姓名（优先按登录账号反查员工资料，保证中文姓名顺序正确）。
        /// </summary>
        private async Task<string> ResolveCurrentOperatorDisplayNameAsync()
        {
            var loginAccount = User.FindFirstValue("erp:account");

            if (!string.IsNullOrWhiteSpace(loginAccount))
            {
                var currentEmployee = (await _hrRepository.GetEmployeesAsync())
                    .FirstOrDefault(employee =>
                        string.Equals(employee.LoginAccount, loginAccount, StringComparison.OrdinalIgnoreCase));

                if (currentEmployee is not null)
                {
                    return BuildDisplayName(currentEmployee);
                }
            }

            return !string.IsNullOrWhiteSpace(User.Identity?.Name)
                ? User.Identity.Name
                : _localizer["Common_Admin"];
        }

        /// <summary>
        /// 准备员工详情页下拉数据（组织、部门、职位及静态字典选项）。
        /// </summary>
        private async Task PopulateSelectListsAsync(Employee? employee = null, bool applyDefaults = false)
        {
            var currentEmployee = employee ?? new Employee();

            var organizations = await _hrRepository.GetCompanyOrganizationsAsync();
            var departments = await _hrRepository.GetDepartmentsAsync();
            var positions = await BuildBasicDataSelectListAsync(PositionTypeCode, currentEmployee.PositionId);
            var ethnicityOptions = await BuildBasicDataSelectListAsync(EthnicityTypeCode, currentEmployee.EthnicityId);
            var professionalTitleOptions = await BuildBasicDataSelectListAsync(ProfessionalTitleTypeCode, currentEmployee.ProfessionalTitleId);
            var locationTypes = await _basicDataRepository.GetTypesAsync();
            var locationItems = (await _basicDataRepository.GetItemsAsync())
                .Where(item => !item.IsDeleted && item.IsActive)
                .ToList();
            var locationTypeIdMap = locationTypes.ToDictionary(type => type.TypeCode, type => type.Id, StringComparer.OrdinalIgnoreCase);

            var selectedCountryRegionId = currentEmployee.CountryRegionId;
            var selectedCityId = currentEmployee.CityId;
            var selectedCountyId = currentEmployee.CountyId;

            NormalizeLocationSelection(
                locationItems,
                locationTypeIdMap,
                ref selectedCountryRegionId,
                ref selectedCityId,
                ref selectedCountyId);

            if (applyDefaults)
            {
                selectedCountryRegionId ??= FindDefaultItemId(locationItems, locationTypeIdMap, CountryRegionTypeCode, "CN");
                selectedCityId ??= FindDefaultChildItemId(locationItems, locationTypeIdMap, CityTypeCode, selectedCountryRegionId, "GUANGZHOU");
                selectedCountyId ??= FindDefaultChildItemId(locationItems, locationTypeIdMap, CountyTypeCode, selectedCityId, "TIANHE");
            }

            currentEmployee.CountryRegionId = selectedCountryRegionId;
            currentEmployee.CityId = selectedCityId;
            currentEmployee.CountyId = selectedCountyId;

            ViewData["OrganizationId"] = new SelectList(organizations, "Id", "OrganizationName", currentEmployee.OrganizationId);
            ViewData["DepartmentId"] = new SelectList(departments, "Id", "Name", currentEmployee.DepartmentId);
            ViewData["PositionId"] = positions;
            ViewData["GroupId"] = BuildStaticSelectList(currentEmployee.GroupId, new[]
            {
                (1, _localizer["Employee_Group_Standard"].Value),
                (2, _localizer["Employee_Group_Business"].Value),
                (3, _localizer["Employee_Group_Admin"].Value),
                (4, _localizer["Employee_Group_Rnd"].Value)
            });
            ViewData["GenderId"] = BuildStaticSelectList(currentEmployee.GenderId, new[]
            {
                (1, _localizer["Employee_Gender_Male"].Value),
                (2, _localizer["Employee_Gender_Female"].Value)
            });
            ViewData["EthnicityId"] = ethnicityOptions;
            ViewData["MaritalStatusId"] = BuildStaticSelectList(currentEmployee.MaritalStatusId, new[]
            {
                (1, _localizer["Employee_MaritalStatus_Single"].Value),
                (2, _localizer["Employee_MaritalStatus_Married"].Value)
            });
            ViewData["EducationLevelId"] = BuildStaticSelectList(currentEmployee.EducationLevelId, new[]
            {
                (1, _localizer["Employee_Education_HighSchool"].Value),
                (2, _localizer["Employee_Education_College"].Value),
                (3, _localizer["Employee_Education_Bachelor"].Value),
                (4, _localizer["Employee_Education_Master"].Value),
                (5, _localizer["Employee_Education_Doctor"].Value)
            });
            ViewData["ProfessionalTitleId"] = professionalTitleOptions;
            ViewData["CountryRegionId"] = BuildFilteredBasicDataSelectList(
                locationItems,
                locationTypeIdMap,
                CountryRegionTypeCode,
                currentEmployee.CountryRegionId);
            ViewData["CityId"] = BuildFilteredBasicDataSelectList(
                locationItems,
                locationTypeIdMap,
                CityTypeCode,
                currentEmployee.CityId,
                currentEmployee.CountryRegionId);
            ViewData["CountyId"] = BuildFilteredBasicDataSelectList(
                locationItems,
                locationTypeIdMap,
                CountyTypeCode,
                currentEmployee.CountyId,
                currentEmployee.CityId);
            ViewData["EmploymentTypeId"] = BuildStaticSelectList(currentEmployee.EmploymentTypeId, new[]
            {
                (1, "\u5168\u804c"),
                (2, "\u517c\u804c"),
                (3, "\u5b9e\u4e60"),
                (4, "\u52b3\u52a1\u6d3e\u9063"),
                (5, "\u7092\u6563")
            });
            ViewData["AllowancePackageId"] = await BuildBasicDataSelectListAsync(AllowancePackageTypeCode, currentEmployee.AllowancePackageId);
            ViewData["AnnualLeaveCalculationMethodId"] = BuildStaticSelectList(currentEmployee.AnnualLeaveCalculationMethodId, new[]
            {
                (1, "\u5e74\u5ea6\u8ba1\u7b97"),
                (2, "\u5165\u804c\u65e5\u8ba1\u7b97"),
                (3, "\u5165\u804c\u6708\u8ba1\u7b97")
            });
            ViewData["DefaultShiftId"] = BuildStaticSelectList(currentEmployee.DefaultShiftId, Array.Empty<(int Value, string Text)>());
            ViewData["SchedulingGroupId"] = await BuildBasicDataSelectListAsync(SchedulingGroupTypeCode, currentEmployee.SchedulingGroupId);
            ViewData["SalaryGradeId"] = BuildStaticSelectList(currentEmployee.SalaryGradeId, Array.Empty<(int Value, string Text)>());
            ViewData["PayrollCompanyId"] = new SelectList(organizations, "Id", "OrganizationName", currentEmployee.PayrollCompanyId);
            ViewData["BankId"] = await BuildBasicDataSelectListAsync(BankTypeCode, currentEmployee.BankId);
        }

        private static Employee CreateDefaultEmployee()
        {
            return new Employee
            {
                HireDate = DateTime.Today,
                GenderId = 1,
                GroupId = 1,
                MaritalStatusId = 1,
                EducationLevelId = 4,
                EmploymentTypeId = 1,
                AnnualLeaveCalculationMethodId = 1,
                IsAttendanceRequired = true
            };
        }

        /// <summary>
        /// 构建静态下拉选项（用于当前暂时无基础字典接口的员工资料页）。
        /// </summary>
        private static List<SelectListItem> BuildStaticSelectList(int? selectedValue, IEnumerable<(int Value, string Text)> options)
        {
            var items = new List<SelectListItem>();
            foreach (var option in options)
            {
                items.Add(new SelectListItem
                {
                    Value = option.Value.ToString(),
                    Text = option.Text,
                    Selected = selectedValue == option.Value
                });
            }

            return items;
        }

        /// <summary>
        /// 构建地址基础数据下拉选项（支持按上级选项过滤城市和县/区）。
        /// </summary>
        private static List<SelectListItem> BuildFilteredBasicDataSelectList(
            IEnumerable<BasicDataItem> activeItems,
            IReadOnlyDictionary<string, int> typeIdMap,
            string typeCode,
            int? selectedValue,
            int? parentId = null)
        {
            if (!typeIdMap.TryGetValue(typeCode, out var typeId))
            {
                return [];
            }

            return activeItems
                .Where(item => item.TypeId == typeId)
                .Where(item => !parentId.HasValue || item.ParentId == parentId.Value)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.ItemName)
                .Select(item => new SelectListItem
                {
                    Value = item.Id.ToString(),
                    Text = item.ItemName,
                    Selected = selectedValue == item.Id
                })
                .ToList();
        }

        /// <summary>
        /// 获取地址基础数据选项（供前端级联下拉按层级动态加载）。
        /// </summary>
        private async Task<List<BasicDataItem>> GetLocationItemsAsync(string typeCode, int? parentId = null)
        {
            var types = await _basicDataRepository.GetTypesAsync();
            var activeItems = (await _basicDataRepository.GetItemsAsync())
                .Where(item => !item.IsDeleted && item.IsActive)
                .ToList();
            var typeIdMap = types.ToDictionary(type => type.TypeCode, type => type.Id, StringComparer.OrdinalIgnoreCase);

            if (!typeIdMap.TryGetValue(typeCode, out var typeId))
            {
                return [];
            }

            return activeItems
                .Where(item => item.TypeId == typeId)
                .Where(item => !parentId.HasValue || item.ParentId == parentId.Value)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.ItemName)
                .ToList();
        }

        /// <summary>
        /// 校验员工地址层级关系（城市必须属于国家/地区，县/区必须属于城市）。
        /// </summary>
        private async Task ValidateLocationSelectionAsync(
            int? countryRegionId,
            int? cityId,
            int? countyId,
            string countryRegionFieldName,
            string cityFieldName,
            string countyFieldName)
        {
            var types = await _basicDataRepository.GetTypesAsync();
            var activeItems = (await _basicDataRepository.GetItemsAsync())
                .Where(item => !item.IsDeleted && item.IsActive)
                .ToList();
            var typeIdMap = types.ToDictionary(type => type.TypeCode, type => type.Id, StringComparer.OrdinalIgnoreCase);

            var countryRegionItem = FindActiveItem(activeItems, typeIdMap, CountryRegionTypeCode, countryRegionId);
            var cityItem = FindActiveItem(activeItems, typeIdMap, CityTypeCode, cityId);
            var countyItem = FindActiveItem(activeItems, typeIdMap, CountyTypeCode, countyId);

            if (countryRegionId.HasValue && countryRegionItem == null)
            {
                ModelState.AddModelError(countryRegionFieldName, "所选国家/地区不存在或已停用。");
            }

            if (cityId.HasValue && cityItem == null)
            {
                ModelState.AddModelError(cityFieldName, "所选城市不存在或已停用。");
            }

            if (countyId.HasValue && countyItem == null)
            {
                ModelState.AddModelError(countyFieldName, "所选县/区不存在或已停用。");
            }

            if (cityItem != null)
            {
                if (!countryRegionId.HasValue)
                {
                    ModelState.AddModelError(countryRegionFieldName, "选择城市前请先选择国家/地区。");
                }
                else if (cityItem.ParentId != countryRegionId.Value)
                {
                    ModelState.AddModelError(cityFieldName, "所选城市不属于当前国家/地区。");
                }
            }

            if (countyItem != null)
            {
                if (!cityId.HasValue)
                {
                    ModelState.AddModelError(cityFieldName, "选择县/区前请先选择城市。");
                }
                else if (countyItem.ParentId != cityId.Value)
                {
                    ModelState.AddModelError(countyFieldName, "所选县/区不属于当前城市。");
                }
            }
        }

        /// <summary>
        /// 规范员工卡号并校验在职员工唯一性（留空允许保存）。
        /// </summary>
        private async Task ValidateEmployeeCardNumberAsync(Employee employee, int? excludedEmployeeId = null)
        {
            employee.CardNumber = NormalizeOptionalText(employee.CardNumber);
            if (string.IsNullOrWhiteSpace(employee.CardNumber))
            {
                return;
            }

            var exists = await _hrRepository.ExistsActiveEmployeeCardNumberAsync(employee.CardNumber, excludedEmployeeId);
            if (exists)
            {
                ModelState.AddModelError(nameof(Employee.CardNumber), "当前员工卡号已被其他在职员工使用。");
            }
        }

        /// <summary>
        /// 判断文档是否为员工图片文档（用于限制主图只能来自受控图片集合）。
        /// </summary>
        private static bool IsEmployeePhotoDocument(ManagedDocument document)
        {
            var contentType = document.ContentType.ToLowerInvariant();
            var extension = document.FileExtension.ToLowerInvariant();
            return contentType is "image/jpeg" or "image/png" or "image/webp" or "image/gif"
                && extension is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif";
        }

        /// <summary>
        /// 规范可空文本输入（去除首尾空格，空白值统一转为 null）。
        /// </summary>
        private static string? NormalizeOptionalText(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        /// <summary>
        /// 归一化地址层级选中值（子级存在时自动补齐父级，父子不匹配时清空无效子级）。
        /// </summary>
        private static void NormalizeLocationSelection(
            IReadOnlyCollection<BasicDataItem> activeItems,
            IReadOnlyDictionary<string, int> typeIdMap,
            ref int? countryRegionId,
            ref int? cityId,
            ref int? countyId)
        {
            var countryRegionItem = FindActiveItem(activeItems, typeIdMap, CountryRegionTypeCode, countryRegionId);
            var cityItem = FindActiveItem(activeItems, typeIdMap, CityTypeCode, cityId);
            var countyItem = FindActiveItem(activeItems, typeIdMap, CountyTypeCode, countyId);

            if (countyId.HasValue && countyItem == null)
            {
                countyId = null;
            }

            if (cityId.HasValue && cityItem == null)
            {
                cityId = null;
            }

            if (countryRegionId.HasValue && countryRegionItem == null)
            {
                countryRegionId = null;
            }

            countyItem = FindActiveItem(activeItems, typeIdMap, CountyTypeCode, countyId);
            if (countyItem != null && !cityId.HasValue)
            {
                cityId = countyItem.ParentId;
            }

            cityItem = FindActiveItem(activeItems, typeIdMap, CityTypeCode, cityId);
            if (cityItem != null && !countryRegionId.HasValue)
            {
                countryRegionId = cityItem.ParentId;
            }

            cityItem = FindActiveItem(activeItems, typeIdMap, CityTypeCode, cityId);
            if (cityItem != null && countryRegionId.HasValue && cityItem.ParentId != countryRegionId.Value)
            {
                cityId = null;
                countyId = null;
            }

            countyItem = FindActiveItem(activeItems, typeIdMap, CountyTypeCode, countyId);
            if (countyItem != null && cityId.HasValue && countyItem.ParentId != cityId.Value)
            {
                countyId = null;
            }
        }

        /// <summary>
        /// 获取指定地址类型的已启用基础数据项（按选中值和类型双重校验）。
        /// </summary>
        private static BasicDataItem? FindActiveItem(
            IEnumerable<BasicDataItem> activeItems,
            IReadOnlyDictionary<string, int> typeIdMap,
            string typeCode,
            int? itemId)
        {
            if (!itemId.HasValue || !typeIdMap.TryGetValue(typeCode, out var typeId))
            {
                return null;
            }

            return activeItems.FirstOrDefault(item => item.Id == itemId.Value && item.TypeId == typeId);
        }

        /// <summary>
        /// 获取指定类型的默认基础数据项 ID（用于新建员工时填充默认地址）。
        /// </summary>
        private static int? FindDefaultItemId(
            IEnumerable<BasicDataItem> activeItems,
            IReadOnlyDictionary<string, int> typeIdMap,
            string typeCode,
            string itemCode)
        {
            if (!typeIdMap.TryGetValue(typeCode, out var typeId))
            {
                return null;
            }

            return activeItems
                .FirstOrDefault(item =>
                    item.TypeId == typeId
                    && string.Equals(item.ItemCode, itemCode, StringComparison.OrdinalIgnoreCase))
                ?.Id;
        }

        /// <summary>
        /// 获取指定父级下的默认子级基础数据项 ID（用于按层级补全默认城市和县/区）。
        /// </summary>
        private static int? FindDefaultChildItemId(
            IEnumerable<BasicDataItem> activeItems,
            IReadOnlyDictionary<string, int> typeIdMap,
            string typeCode,
            int? parentId,
            string itemCode)
        {
            if (!parentId.HasValue || !typeIdMap.TryGetValue(typeCode, out var typeId))
            {
                return null;
            }

            return activeItems
                .FirstOrDefault(item =>
                    item.TypeId == typeId
                    && item.ParentId == parentId.Value
                    && string.Equals(item.ItemCode, itemCode, StringComparison.OrdinalIgnoreCase))
                ?.Id;
        }

        /// <summary>
        /// 按基础数据类型编码构建下拉选项（用于民族、职称等用户可维护字典）。
        /// </summary>
        private async Task<List<SelectListItem>> BuildBasicDataSelectListAsync(string typeCode, int? selectedValue)
        {
            var type = await _basicDataRepository.GetTypeByCodeAsync(typeCode);
            if (type == null)
            {
                return [];
            }

            var items = await _basicDataRepository.GetItemsByTypeIdAsync(type.Id);
            return items
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.ItemName)
                .Select(item => new SelectListItem
                {
                    Value = item.Id.ToString(),
                    Text = item.ItemName,
                    Selected = selectedValue == item.Id
                })
                .ToList();
        }

        /// <summary>
        /// 返回员工详情页关闭脚本（通知父页面关闭弹层并刷新列表）。
        /// </summary>
        private ContentResult BuildDetailPageCloseResult()
        {
            var title = _localizer["Common_Processing"];
            var html = $$"""
                <!DOCTYPE html>
                <html lang="zh-CN">
                <head>
                    <meta charset="utf-8" />
                    <title>{{title}}</title>
                </head>
                <body>
                    <script>
                        const closeMessage = {
                            type: "open-erp:employee-modal-close",
                            refreshRequested: true
                        };

                        if (window.parent && window.parent !== window) {
                            window.parent.postMessage(closeMessage, window.location.origin);
                        } else if (window.opener && !window.opener.closed) {
                            window.opener.postMessage(closeMessage, window.location.origin);
                            window.close();
                        }
                    </script>
                </body>
                </html>
                """;

            return Content(html, "text/html; charset=utf-8");
        }
    }
}
