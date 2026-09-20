using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenERP.HR.Models.Entities;
using OpenERP.Web.Data.HR;

namespace OpenERP.Web.Controllers;

/// <summary>
/// 员工培训历程 API 控制器（提供员工工作/培训/教育经历的查询、保存与删除接口）。
/// </summary>
[ApiController]
[Route("api/employeetrainingexperience")]
[Authorize]
public class EmployeeTrainingExperienceApiController : ControllerBase
{
    /// <summary>
    /// 历程类型选项（统一用于查询筛选与编辑下拉）。
    /// </summary>
    private static readonly IReadOnlyList<EmployeeTrainingExperienceOptionItem> ExperienceTypeOptions =
    [
        new() { Code = "WORK", Label = "工作经历" },
        new() { Code = "TRAINING", Label = "培训经历" },
        new() { Code = "EDUCATION", Label = "教育经历" }
    ];

    /// <summary>
    /// 人资仓储（负责员工培训历程数据读写）。
    /// </summary>
    private readonly IHrRepository _hrRepository;

    public EmployeeTrainingExperienceApiController(IHrRepository hrRepository)
    {
        _hrRepository = hrRepository;
    }

    /// <summary>
    /// 查询员工培训历程分页数据（按员工、历程类型与关键字返回工作/培训/教育经历）。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        int employeeId,
        string? keyword = null,
        string? experienceTypeCode = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        if (employeeId <= 0)
        {
            return BadRequest(new { message = "员工ID不能为空。" });
        }

        var safePageNumber = pageNumber <= 0 ? 1 : pageNumber;
        var safePageSize = pageSize <= 0 ? 10 : pageSize;
        var pageResult = await _hrRepository.GetEmployeeTrainingExperiencesAsync(
            employeeId,
            keyword,
            experienceTypeCode,
            safePageNumber,
            safePageSize);

        return Ok(new
        {
            pageNumber = safePageNumber,
            pageSize = safePageSize,
            totalCount = pageResult.TotalCount,
            experienceTypeOptions = ExperienceTypeOptions,
            items = pageResult.Records.Select((item, index) => new
            {
                item.Id,
                sequenceNo = (safePageNumber - 1) * safePageSize + index + 1,
                item.ExperienceTypeCode,
                experienceTypeLabel = ResolveOptionLabel(ExperienceTypeOptions, item.ExperienceTypeCode),
                startDate = item.StartDate.ToString("yyyy-MM-dd"),
                endDate = item.EndDate?.ToString("yyyy-MM-dd"),
                item.Description,
                item.CertificateName,
                item.OrganizationName,
                item.ArchivePath,
                archiveLabel = "档案",
                item.CreatedBy,
                createdAt = item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            })
        });
    }

    /// <summary>
    /// 保存员工培训历程记录（支持新增与编辑）。
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveEmployeeTrainingExperienceRequest request)
    {
        if (request.EmployeeId <= 0)
        {
            return BadRequest(new { message = "员工ID不能为空。" });
        }

        if (request.StartDate == null)
        {
            return BadRequest(new { message = "开始日期不能为空。" });
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { message = "历程描述不能为空。" });
        }

        if (request.EndDate.HasValue && request.EndDate.Value.Date < request.StartDate.Value.Date)
        {
            return BadRequest(new { message = "结束日期不能早于开始日期。" });
        }

        var normalizedExperienceTypeCode = NormalizeOptionCode(ExperienceTypeOptions, request.ExperienceTypeCode, "TRAINING");
        var operatorDisplayName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("erp:account") ?? "System";

        var experience = new EmployeeTrainingExperience
        {
            Id = request.Id ?? 0,
            EmployeeId = request.EmployeeId,
            ExperienceTypeCode = normalizedExperienceTypeCode,
            StartDate = request.StartDate.Value.Date,
            EndDate = request.EndDate?.Date,
            Description = request.Description.Trim(),
            CertificateName = request.CertificateName?.Trim(),
            OrganizationName = request.OrganizationName?.Trim(),
            ArchivePath = request.ArchivePath?.Trim(),
            CreatedBy = operatorDisplayName,
            UpdatedBy = operatorDisplayName
        };

        var savedId = await _hrRepository.SaveEmployeeTrainingExperienceAsync(experience);
        if (savedId <= 0)
        {
            return BadRequest(new { message = "保存培训历程失败，请确认记录仍然存在后重试。" });
        }

        return Ok(new { id = savedId, message = request.Id > 0 ? "培训历程已更新。" : "培训历程已新增。" });
    }

    /// <summary>
    /// 删除员工培训历程记录（支持当前员工名下的批量删除）。
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteEmployeeTrainingExperiencesRequest request)
    {
        if (request.EmployeeId <= 0)
        {
            return BadRequest(new { message = "员工ID不能为空。" });
        }

        var deletedCount = await _hrRepository.DeleteEmployeeTrainingExperiencesAsync(
            request.EmployeeId,
            request.Ids ?? []);

        return Ok(new { deletedCount, message = deletedCount > 0 ? "培训历程已删除。" : "未找到可删除的培训历程记录。" });
    }

    /// <summary>
    /// 解析选项显示名（根据选项编码返回界面标签）。
    /// </summary>
    private static string ResolveOptionLabel(IReadOnlyList<EmployeeTrainingExperienceOptionItem> options, string? code)
        => options.FirstOrDefault(item => string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase))?.Label ?? string.Empty;

    /// <summary>
    /// 归一化选项编码（非法编码回退为默认值）。
    /// </summary>
    private static string NormalizeOptionCode(IReadOnlyList<EmployeeTrainingExperienceOptionItem> options, string? code, string defaultCode)
        => options.Any(item => string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase))
            ? code!.Trim().ToUpperInvariant()
            : defaultCode;
}

/// <summary>
/// 员工培训历程选项项（用于历程类型下拉）。
/// </summary>
public class EmployeeTrainingExperienceOptionItem
{
    /// <summary>
    /// 选项编码（存储到员工培训历程表中的固定编码）。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 选项名称（界面展示名称）。
    /// </summary>
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// 保存员工培训历程请求体（承载新增或编辑时提交的历程字段）。
/// </summary>
public class SaveEmployeeTrainingExperienceRequest
{
    /// <summary>
    /// 培训历程ID（为空表示新增，非空表示编辑）。
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// 员工ID（对应 Employee 实体主键）。
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// 历程类型编码（固定选项编码）。
    /// </summary>
    public string? ExperienceTypeCode { get; set; }

    /// <summary>
    /// 开始日期（历程开始日期）。
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 结束日期（历程结束日期，可为空）。
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 历程描述（工作、培训或教育内容说明）。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 荣誉证书或培训证书（可为空）。
    /// </summary>
    public string? CertificateName { get; set; }

    /// <summary>
    /// 培训/工作机构名称（可为空）。
    /// </summary>
    public string? OrganizationName { get; set; }

    /// <summary>
    /// 档案路径（附件或档案入口，可为空）。
    /// </summary>
    public string? ArchivePath { get; set; }
}

/// <summary>
/// 删除员工培训历程请求体（承载当前员工下选中的记录ID）。
/// </summary>
public class DeleteEmployeeTrainingExperiencesRequest
{
    /// <summary>
    /// 员工ID（对应 Employee 实体主键）。
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// 待删除ID列表（仅删除这些选中的培训历程记录）。
    /// </summary>
    public List<int>? Ids { get; set; }
}
