/*
 * �ļ���OpenERP.Web/Areas/HR/Controllers/EmployeesController.cs
 * ˵����Ա������������������Ա�����ϡ�Ա����ͼ�����ҳ������
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
        /// ���ʲִ�����ȡԱ������֯��������ְλ���ݣ���
        /// </summary>
        private readonly IHrRepository _hrRepository;

        /// <summary>
        /// �������ݲִ�����ȡ���塢ְ�Ƶ��û��Զ����������ݣ���
        /// </summary>
        private readonly IBasicDataRepository _basicDataRepository;

        /// <summary>
        /// �������ػ���Դ������Ա��ҳ����������ʻ�����
        /// </summary>
        private readonly IStringLocalizer<SharedResource> _localizer;

        /// <summary>
        /// ͨ���ĵ�������������У��Ա��ͼƬ�ĵ��Ƿ����ڵ�ǰԱ������
        /// </summary>
        private readonly ICommonDocumentService _documentService;

        /// <summary>
        /// Ա��ͼƬ�ĵ����ܱ��루��ӦԱ��������ͼƬ���ϴ���ͼƬ���ϣ���
        /// </summary>
        private const string EmployeePhotoFeatureCode = "HR_EMPLOYEE_PHOTO";

        /// <summary>
        /// ְλ�����������ͱ��루��Ӧ���������ֵ� POSITION����
        /// </summary>
        private const string PositionTypeCode = "POSITION";

        /// <summary>
        /// ��������������ͱ��루��Ӧ���������ֵ� ETHNICITY����
        /// </summary>
        private const string EthnicityTypeCode = "ETHNICITY";

        /// <summary>
        /// ְ�ƻ����������ͱ��루��Ӧ���������ֵ� PROFESSIONAL_TITLE����
        /// </summary>
        private const string ProfessionalTitleTypeCode = "PROFESSIONAL_TITLE";

        /// <summary>
        /// ����/���������������ͱ��루��ӦԱ������/������������
        /// </summary>
        private const string CountryRegionTypeCode = "COUNTRY_REGION";

        /// <summary>
        /// ���л����������ͱ��루��ӦԱ��������������
        /// </summary>
        private const string CityTypeCode = "CITY";

        /// <summary>
        /// ��/�������������ͱ��루��ӦԱ����/����������
        /// </summary>
        private const string CountyTypeCode = "COUNTY";

        /// <summary>
        /// �������������������ͱ��루��Ӧ���������ֵ� ALLOWANCE_PACKAGE����
        /// </summary>
        private const string AllowancePackageTypeCode = "ALLOWANCE_PACKAGE";

        /// <summary>
        /// �Ű��������������ͱ��루��Ӧ���������ֵ� SCHEDULING_GROUP����
        /// </summary>
        private const string SchedulingGroupTypeCode = "SCHEDULING_GROUP";

        /// <summary>
        /// ���л����������ͱ��루��Ӧ���������ֵ� BANK����
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
        /// ��Ա�����б�ҳ��ʹ��ҳ��ģ�ͳ���չʾ���ݣ���
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
        /// ����Ա��ҳ�棨�ɴ��� copyFromId ������Ա���������ϣ���
        /// </summary>
        public async Task<IActionResult> Create(bool popup = false, int? copyFromId = null)
        {
            Employee employee;

            if (copyFromId.HasValue && copyFromId.Value > 0)
            {
                // ����ԴԱ���������ϣ������������Ψһ��ʶ�ֶΡ�
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
            [Bind("EmployeeCode,FirstName,LastName,OrganizationId,Email,PhoneNumber,PhoneNumber2,PhoneNumber3,JobTitle,DepartmentId,PositionId,GroupId,GenderId,AliasName,BirthDate,IdCardNumber,CardNumber,EthnicityId,MaritalStatusId,EducationLevelId,EducationCertificateNumber,ProfessionalTitleId,CountryRegionId,CityId,CountyId,Address,Remarks,EmergencyContact,EmergencyContactPhone,Referrer,ArchivePath,PhotoPath,HireDate,LeaveDate,EmploymentTypeId,AllowancePackageId,ProbationEndDate,AnnualLeaveCalculationMethodId,IsAttendanceRequired,CurrentYearAnnualLeaveDays,AnnualLeaveMaxAccumulatedDays,AnnualLeaveRemainingDays,AnnualLeaveIncrementStartYears,AnnualLeaveIncrementPerYearDays,AnnualLeaveCapDays,DefaultShiftId,SchedulingGroupId,IsAutoSchedulingEnabled,SalaryGradeId,PayrollCompanyId,BankAccountNumber,BankAccountName,BankId")]
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
            [Bind("Id,EmployeeCode,FirstName,LastName,OrganizationId,Email,PhoneNumber,PhoneNumber2,PhoneNumber3,JobTitle,DepartmentId,PositionId,GroupId,GenderId,AliasName,BirthDate,IdCardNumber,CardNumber,EthnicityId,MaritalStatusId,EducationLevelId,EducationCertificateNumber,ProfessionalTitleId,CountryRegionId,CityId,CountyId,Address,Remarks,EmergencyContact,EmergencyContactPhone,Referrer,ArchivePath,PhotoPath,HireDate,LeaveDate,EmploymentTypeId,AllowancePackageId,ProbationEndDate,AnnualLeaveCalculationMethodId,IsAttendanceRequired,CurrentYearAnnualLeaveDays,AnnualLeaveMaxAccumulatedDays,AnnualLeaveRemainingDays,AnnualLeaveIncrementStartYears,AnnualLeaveIncrementPerYearDays,AnnualLeaveCapDays,DefaultShiftId,SchedulingGroupId,IsAutoSchedulingEnabled,SalaryGradeId,PayrollCompanyId,BankAccountNumber,BankAccountName,BankId")]
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
        /// Ա������鿴ҳ�棨ֻ��ģʽ�������༭�����޸ģ���
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
        /// Ա��ɾ��ȷ��ҳ�棨չʾԱ����Ϣ��ȷ�Ϻ�ִ����ɾ������
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
        /// ȷ��ɾ��Ա������� IsDeleted Ϊ��ɾ������
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, bool popup = false)
        {
            await _hrRepository.DeleteEmployeeAsync(id);
            return popup ? BuildDetailPageCloseResult() : RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// ��ȡԱ����ַ��������ѡ�������/��������й����¼����ݣ���
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
        /// ����Ա����ͼ����Ա��ͼƬ�ĵ�������ѡ��һ��ͼƬд�� PhotoPath����
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimaryPhoto([FromForm] int employeeId, [FromForm] int? documentId)
        {
            if (employeeId <= 0)
            {
                return BadRequest(new { message = "���ȱ���Ա���������ϣ�������Ա����ͼ��" });
            }

            var employee = await _hrRepository.GetEmployeeByIdAsync(employeeId);
            if (employee == null)
            {
                return NotFound(new { message = "δ�ҵ�Ա�����ϡ�" });
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
                    return BadRequest(new { message = "��ѡ��ǰԱ�����µ���ЧͼƬ�ĵ���" });
                }

                photoPath = $"/api/documents/{document.Id}/open";
            }

            var updatedBy = await ResolveCurrentOperatorDisplayNameAsync();
            var updated = await _hrRepository.UpdateEmployeePhotoPathAsync(employeeId, photoPath, updatedBy);
            if (!updated)
            {
                return NotFound(new { message = "δ�ҵ��ɸ��µ�Ա�����ϡ�" });
            }

            return Ok(new
            {
                message = string.IsNullOrWhiteSpace(photoPath) ? "Ա����ͼ�������" : "Ա����ͼ�Ѹ��¡�",
                photoPath
            });
        }

        /// <summary>
        /// ����Ա���б�����ͼģ�ͣ�����ʵ�嵽ҳ��չʾ�ֶε�ת������
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
        /// ����Ա��չʾ��������������ֱ��ƴ�ӣ�Ӣ�����������ո�
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
        /// ����Ա����ţ�δά��ʱ����Ų�����ʾ��
        /// </summary>
        private static string BuildEmployeeCode(Employee employee, int sequenceNo)
            => !string.IsNullOrWhiteSpace(employee.EmployeeCode) ? employee.EmployeeCode : $"A{sequenceNo:000}";

        /// <summary>
        /// ����Ա����ְ״̬�ı���
        /// </summary>
        private string BuildEmploymentStatus(Employee employee)
            => employee.IsDeleted
                ? _localizer["Employee_Status_Disabled"]
                : employee.LeaveDate.HasValue
                    ? _localizer["Employee_Status_Left"]
                    : _localizer["Employee_Status_Active"];

        /// <summary>
        /// �����Ա��ı���δά��ʱ��ʾĬ��ֵ��
        /// </summary>
        private string BuildGenderLabel(Employee employee)
            => employee.GenderId switch
            {
                2 => _localizer["Employee_Gender_Female"],
                _ => _localizer["Employee_Gender_Male"]
            };

        /// <summary>
        /// ����������֯���ƣ�����չʾ��֯��ơ�
        /// </summary>
        private string BuildOrganizationName(Employee employee)
        {
            var organizationName = employee.Organization?.OrganizationName;

            if (string.IsNullOrWhiteSpace(organizationName))
            {
                return _localizer["Employee_DefaultOrganizationName"];
            }

            foreach (var removableSuffix in new[] { "�ɷ����޹�˾", "�������޹�˾", "�������ι�˾", "���޹�˾" })
            {
                if (organizationName.EndsWith(removableSuffix, StringComparison.Ordinal))
                {
                    return organizationName[..^removableSuffix.Length];
                }
            }

            return organizationName;
        }

        /// <summary>
        /// ���ɲ������ƣ�δ����ʱʹ��Ĭ��ֵ��
        /// </summary>
        private string BuildDepartmentLabel(Employee employee)
            => employee.Department?.Name ?? _localizer["Employee_DefaultDepartment"];

        /// <summary>
        /// ����������ƣ�δ����ʱ��ʾĬ�����
        /// </summary>
        private string BuildGroupLabel(Employee employee)
            => employee.GroupId.HasValue
                ? _localizer["Employee_GroupPattern", employee.GroupId.Value]
                : _localizer["Employee_DefaultGroup"];

        /// <summary>
        /// ����ְ��չʾ���ݣ�δά��ʱʹ��Ĭ��ְ��
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
        /// ���ɵ绰չʾ���ݣ�δά��ʱʹ����ʾ���롣
        /// </summary>
        private static string BuildPhoneLabel(Employee employee)
            => employee.PhoneNumber ?? "(020) 6666 8888";

        /// <summary>
        /// ���ɴ���չʾ���ݣ�δά��ʱʹ�õ绰������ʾ���롣
        /// </summary>
        private static string BuildFaxLabel(Employee employee)
            => employee.PhoneNumber2 ?? "(020) 6666 8889";

        /// <summary>
        /// ��������չʾ���ݣ�δά��ʱʹ����ʾ���䡣
        /// </summary>
        private static string BuildEmailLabel(Employee employee)
            => string.IsNullOrWhiteSpace(employee.Email) ? "jeky@honeyi.com" : employee.Email;

        /// <summary>
        /// ���ɱ�עչʾ���ݣ�δ��дʱ���ؿ��ı���
        /// </summary>
        private string BuildRemarkLabel(Employee employee)
            => string.IsNullOrWhiteSpace(employee.Remarks) ? string.Empty : employee.Remarks;

        /// <summary>
        /// �ж��ַ������Ƿ����Ӣ����ĸ������������ʾ��ʽ�л���
        /// </summary>
        private static bool ContainsLatinLetter(string value)
            => value.Any(character => character is >= 'A' and <= 'Z' or >= 'a' and <= 'z');

        /// <summary>
        /// ������ǰ��¼�����˵���ʾ���������Ȱ���¼�˺ŷ���Ա�����ϣ���֤��������˳����ȷ����
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
        /// ׼��Ա������ҳ�������ݣ���֯�����š�ְλ����̬�ֵ�ѡ���
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
        /// ������̬����ѡ����ڵ�ǰ��ʱ�޻����ֵ�ӿڵ�Ա������ҳ����
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
        /// ������ַ������������ѡ�֧�ְ��ϼ�ѡ����˳��к���/������
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
        /// ��ȡ��ַ��������ѡ���ǰ�˼����������㼶��̬���أ���
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
        /// У��Ա����ַ�㼶��ϵ�����б������ڹ���/��������/���������ڳ��У���
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
                ModelState.AddModelError(countryRegionFieldName, "��ѡ����/���������ڻ���ͣ�á�");
            }

            if (cityId.HasValue && cityItem == null)
            {
                ModelState.AddModelError(cityFieldName, "��ѡ���в����ڻ���ͣ�á�");
            }

            if (countyId.HasValue && countyItem == null)
            {
                ModelState.AddModelError(countyFieldName, "��ѡ��/�������ڻ���ͣ�á�");
            }

            if (cityItem != null)
            {
                if (!countryRegionId.HasValue)
                {
                    ModelState.AddModelError(countryRegionFieldName, "ѡ�����ǰ����ѡ�����/������");
                }
                else if (cityItem.ParentId != countryRegionId.Value)
                {
                    ModelState.AddModelError(cityFieldName, "��ѡ���в����ڵ�ǰ����/������");
                }
            }

            if (countyItem != null)
            {
                if (!cityId.HasValue)
                {
                    ModelState.AddModelError(cityFieldName, "ѡ����/��ǰ����ѡ����С�");
                }
                else if (countyItem.ParentId != cityId.Value)
                {
                    ModelState.AddModelError(countyFieldName, "��ѡ��/�������ڵ�ǰ���С�");
                }
            }
        }

        /// <summary>
        /// �淶Ա�����Ų�У����ְԱ��Ψһ�ԣ������������棩��
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
                ModelState.AddModelError(nameof(Employee.CardNumber), "��ǰԱ�������ѱ�������ְԱ��ʹ�á�");
            }
        }

        /// <summary>
        /// �ж��ĵ��Ƿ�ΪԱ��ͼƬ�ĵ�������������ͼֻ�������ܿ�ͼƬ���ϣ���
        /// </summary>
        private static bool IsEmployeePhotoDocument(ManagedDocument document)
        {
            var contentType = document.ContentType.ToLowerInvariant();
            var extension = document.FileExtension.ToLowerInvariant();
            return contentType is "image/jpeg" or "image/png" or "image/webp" or "image/gif"
                && extension is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif";
        }

        /// <summary>
        /// �淶�ɿ��ı����루ȥ����β�ո񣬿հ�ֵͳһתΪ null����
        /// </summary>
        private static string? NormalizeOptionalText(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        /// <summary>
        /// ��һ����ַ�㼶ѡ��ֵ���Ӽ�����ʱ�Զ����븸�������Ӳ�ƥ��ʱ�����Ч�Ӽ�����
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
        /// ��ȡָ����ַ���͵������û����������ѡ��ֵ������˫��У�飩��
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
        /// ��ȡָ�����͵�Ĭ�ϻ��������� ID�������½�Ա��ʱ���Ĭ�ϵ�ַ����
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
        /// ��ȡָ�������µ�Ĭ���Ӽ����������� ID�����ڰ��㼶��ȫĬ�ϳ��к���/������
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
        /// �������������ͱ��빹������ѡ��������塢ְ�Ƶ��û���ά���ֵ䣩��
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
        /// ����Ա������ҳ�رսű���֪ͨ��ҳ��رյ��㲢ˢ���б�����
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
