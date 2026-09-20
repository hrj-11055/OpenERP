using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenERP.HR.Models.Entities;
using OpenERP.Web.Data.HR;

namespace OpenERP.Web.Controllers;

/// <summary>
/// 公司组织员工记录接口控制器（提供公司组织详细页“员工记录”页签的数据查询）。
/// </summary>
[ApiController]
[Route("api/companyorganizationemployee")]
[Authorize]
public class CompanyOrganizationEmployeeApiController : ControllerBase
{
    /// <summary>
    /// 员工状态筛选固定选项（全部、在职、离职）。
    /// </summary>
    private static readonly IReadOnlyList<CompanyOrganizationEmployeeStatusOptionItem> StatusOptions =
    [
        new("ACTIVE", "\u5728\u804c"),
        new("LEFT", "\u79bb\u804c")
    ];

    /// <summary>
    /// 人资仓储（读取公司组织与员工主数据）。
    /// </summary>
    private readonly IHrRepository _hrRepository;

    /// <summary>
    /// 初始化公司组织员工记录接口控制器。
    /// </summary>
    public CompanyOrganizationEmployeeApiController(IHrRepository hrRepository)
    {
        _hrRepository = hrRepository;
    }

    /// <summary>
    /// 查询指定公司组织下的员工记录分页数据（包含离职员工）。
    /// </summary>
    [HttpGet("{companyOrganizationId:int}")]
    public async Task<IActionResult> Get(
        int companyOrganizationId,
        [FromQuery] string? keyword = null,
        [FromQuery] string? statusCode = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (companyOrganizationId <= 0)
        {
            return BadRequest(new { message = "\u8bf7\u5148\u4fdd\u5b58\u516c\u53f8\u7ec4\u7ec7\u540e\u518d\u67e5\u770b\u5458\u5de5\u8bb0\u5f55\u3002" });
        }

        var organization = await _hrRepository.GetCompanyOrganizationByIdAsync(companyOrganizationId);
        if (organization is null)
        {
            return NotFound(new { message = "\u516c\u53f8\u7ec4\u7ec7\u4e0d\u5b58\u5728\u6216\u5df2\u88ab\u5220\u9664\u3002" });
        }

        var normalizedKeyword = keyword?.Trim();
        var normalizedStatusCode = NormalizeStatusCode(statusCode);
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize <= 0 ? 10 : pageSize;

        var employees = (await _hrRepository.GetEmployeesAsync())
            .Where(employee => employee.OrganizationId == companyOrganizationId && !employee.IsDeleted)
            .Where(employee => MatchesStatus(employee, normalizedStatusCode))
            .Where(employee => string.IsNullOrWhiteSpace(normalizedKeyword) || MatchesKeyword(employee, normalizedKeyword))
            .OrderBy(employee => employee.LeaveDate.HasValue ? 1 : 0)
            .ThenBy(employee => employee.Department?.Name ?? string.Empty)
            .ThenBy(employee => employee.EmployeeCode ?? string.Empty)
            .ThenBy(employee => employee.Id)
            .ToList();

        var totalCount = employees.Count;
        var items = employees
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .Select((employee, index) => new
            {
                employee.Id,
                sequenceNo = (safePageNumber - 1) * safePageSize + index + 1,
                employeeCode = BuildEmployeeCode(employee, index + 1),
                displayName = BuildEmployeeDisplayName(employee),
                genderName = BuildGenderName(employee),
                cardNumber = NormalizeDisplayText(employee.CardNumber),
                departmentName = employee.Department?.Name ?? "-",
                groupName = BuildGroupName(employee),
                positionName = BuildPositionName(employee),
                mobile = NormalizeDisplayText(employee.PhoneNumber),
                telephone = NormalizeDisplayText(employee.PhoneNumber2 ?? employee.PhoneNumber3),
                email = NormalizeDisplayText(employee.Email),
                hasPhoto = !string.IsNullOrWhiteSpace(employee.PhotoPath),
                photoPath = employee.PhotoPath ?? string.Empty,
                remarks = NormalizeDisplayText(employee.Remarks),
                statusName = employee.LeaveDate.HasValue ? "\u79bb\u804c" : "\u5728\u804c",
                detailsUrl = Url.Action("Details", "Employees", new { area = "HR", id = employee.Id, popup = true }) ?? "#"
            })
            .ToList();

        return Ok(new
        {
            pageNumber = safePageNumber,
            pageSize = safePageSize,
            totalCount,
            items,
            statusOptions = StatusOptions
        });
    }

    /// <summary>
    /// 规范状态筛选编码（仅允许 ACTIVE、LEFT）。
    /// </summary>
    private static string? NormalizeStatusCode(string? statusCode)
    {
        var normalizedStatusCode = statusCode?.Trim().ToUpperInvariant();
        return normalizedStatusCode is "ACTIVE" or "LEFT"
            ? normalizedStatusCode
            : null;
    }

    /// <summary>
    /// 判断员工是否符合状态筛选条件。
    /// </summary>
    private static bool MatchesStatus(Employee employee, string? statusCode)
    {
        return statusCode switch
        {
            "ACTIVE" => !employee.LeaveDate.HasValue,
            "LEFT" => employee.LeaveDate.HasValue,
            _ => true
        };
    }

    /// <summary>
    /// 判断员工记录是否命中关键字。
    /// </summary>
    private static bool MatchesKeyword(Employee employee, string keyword)
    {
        var candidateValues = new[]
        {
            employee.EmployeeCode,
            BuildEmployeeDisplayName(employee),
            employee.CardNumber,
            employee.Department?.Name,
            BuildGroupName(employee),
            BuildPositionName(employee),
            employee.PhoneNumber,
            employee.PhoneNumber2,
            employee.PhoneNumber3,
            employee.Email,
            employee.Remarks
        };

        return candidateValues.Any(value =>
            !string.IsNullOrWhiteSpace(value)
            && value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 生成员工编号显示文本（未维护时按序号补位）。
    /// </summary>
    private static string BuildEmployeeCode(Employee employee, int sequenceNo)
        => !string.IsNullOrWhiteSpace(employee.EmployeeCode)
            ? employee.EmployeeCode.Trim()
            : $"A{sequenceNo:000}";

    /// <summary>
    /// 生成员工姓名显示文本（中文姓名直接拼接，英文姓名保留空格）。
    /// </summary>
    private static string BuildEmployeeDisplayName(Employee employee)
    {
        var firstName = employee.FirstName?.Trim() ?? string.Empty;
        var lastName = employee.LastName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
        {
            return "\u672a\u547d\u540d";
        }

        return ContainsLatinLetter(firstName) || ContainsLatinLetter(lastName)
            ? string.Join(" ", new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value)))
            : $"{lastName}{firstName}";
    }

    /// <summary>
    /// 生成员工性别显示文本。
    /// </summary>
    private static string BuildGenderName(Employee employee)
        => employee.GenderId switch
        {
            1 => "\u7537",
            2 => "\u5973",
            _ => "-"
        };

    /// <summary>
    /// 生成组别显示文本（沿用员工资料中的组别编号含义）。
    /// </summary>
    private static string BuildGroupName(Employee employee)
        => employee.GroupId switch
        {
            1 => "\u6807\u51c6\u7ec4",
            2 => "\u4e1a\u52a1\u7ec4",
            3 => "\u884c\u653f\u7ec4",
            4 => "\u7814\u53d1\u7ec4",
            > 0 => $"\u7b2c{employee.GroupId}\u7ec4",
            _ => "-"
        };

    /// <summary>
    /// 生成职位显示文本（优先职位，再回退岗位名称）。
    /// </summary>
    private static string BuildPositionName(Employee employee)
        => !string.IsNullOrWhiteSpace(employee.Position?.Name)
            ? employee.Position.Name
            : NormalizeDisplayText(employee.JobTitle);

    /// <summary>
    /// 判断文本中是否包含拉丁字母（用于姓名拼接规则）。
    /// </summary>
    private static bool ContainsLatinLetter(string value)
        => value.Any(character => character is >= 'A' and <= 'Z' or >= 'a' and <= 'z');

    /// <summary>
    /// 规范前端显示文本（空值统一显示为短横线）。
    /// </summary>
    private static string NormalizeDisplayText(string? value)
        => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
}

/// <summary>
/// 公司组织员工状态选项项（用于员工记录页签状态下拉框）。
/// </summary>
public class CompanyOrganizationEmployeeStatusOptionItem
{
    /// <summary>
    /// 选项值（状态编码）。
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 选项文本（界面显示名称）。
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 初始化空的状态选项项。
    /// </summary>
    public CompanyOrganizationEmployeeStatusOptionItem()
    {
    }

    /// <summary>
    /// 使用指定值和文本初始化状态选项项。
    /// </summary>
    public CompanyOrganizationEmployeeStatusOptionItem(string value, string text)
    {
        Value = value;
        Text = text;
    }
}
