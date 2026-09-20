using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenERP.HR.Models.Entities;
using OpenERP.Web.Data.HR;

namespace OpenERP.Web.Controllers;

/// <summary>
/// 通用任务跟进 API 控制器（提供跨功能复用的任务跟进查询、保存与删除接口）。
/// </summary>
[ApiController]
[Route("api/taskfollowup")]
[Authorize]
public class TaskFollowUpApiController : ControllerBase
{
    /// <summary>
    /// 任务状态选项（统一用于任务查询筛选与编辑下拉）。
    /// </summary>
    private static readonly IReadOnlyList<TaskFollowUpOptionItem> StatusOptions =
    [
        new() { Code = "PENDING", Label = "未完成" },
        new() { Code = "COMPLETED", Label = "已完成" }
    ];

    /// <summary>
    /// 任务类型选项（统一用于任务记录类型下拉）。
    /// </summary>
    private static readonly IReadOnlyList<TaskFollowUpOptionItem> TaskTypeOptions =
    [
        new() { Code = "TASK", Label = "任务" },
        new() { Code = "REMINDER", Label = "提示" },
        new() { Code = "FOLLOW_UP", Label = "跟进" }
    ];

    /// <summary>
    /// 优先级选项（统一用于优先级下拉）。
    /// </summary>
    private static readonly IReadOnlyList<TaskFollowUpOptionItem> PriorityOptions =
    [
        new() { Code = "NORMAL", Label = "普通" },
        new() { Code = "URGENT", Label = "紧急" },
        new() { Code = "CRITICAL", Label = "特急" }
    ];

    /// <summary>
    /// 人资仓储（当前版本复用仓储层完成通用任务跟进读写）。
    /// </summary>
    private readonly IHrRepository _hrRepository;

    public TaskFollowUpApiController(IHrRepository hrRepository)
    {
        _hrRepository = hrRepository;
    }

    /// <summary>
    /// 查询任务跟进分页数据（按功能编码与所属个体加载当前资料的任务记录）。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        string featureCode,
        int entityId,
        string? keyword = null,
        string? statusCode = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(featureCode) || entityId <= 0)
        {
            return BadRequest(new { message = "功能编码和所属个体ID不能为空。" });
        }

        var pageResult = await _hrRepository.GetTaskFollowUpsAsync(featureCode, entityId, keyword, statusCode, pageNumber, pageSize);

        return Ok(new
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber,
            pageSize = pageSize <= 0 ? 10 : pageSize,
            totalCount = pageResult.TotalCount,
            statusOptions = StatusOptions,
            taskTypeOptions = TaskTypeOptions,
            priorityOptions = PriorityOptions,
            items = pageResult.Records.Select((item, index) => new
            {
                item.Id,
                sequenceNo = ((pageNumber <= 0 ? 1 : pageNumber) - 1) * (pageSize <= 0 ? 10 : pageSize) + index + 1,
                item.TaskCode,
                archiveLabel = string.IsNullOrWhiteSpace(item.ArchivePath) ? "查看" : "查看",
                item.ArchivePath,
                item.StatusCode,
                statusLabel = ResolveOptionLabel(StatusOptions, item.StatusCode),
                plannedDate = item.PlannedDate?.ToString("yyyy-MM-dd"),
                item.TaskTypeCode,
                taskTypeLabel = ResolveOptionLabel(TaskTypeOptions, item.TaskTypeCode),
                item.ExecutorName,
                item.Description,
                item.PriorityCode,
                priorityLabel = ResolveOptionLabel(PriorityOptions, item.PriorityCode),
                item.ProgressPercent,
                completedDate = item.CompletedDate?.ToString("yyyy-MM-dd"),
                item.ProjectCode,
                item.InitiatorName,
                createdAt = item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            })
        });
    }

    /// <summary>
    /// 保存任务跟进记录（支持新增与编辑）。
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveTaskFollowUpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FeatureCode) || request.EntityId <= 0)
        {
            return BadRequest(new { message = "功能编码和所属个体ID不能为空。" });
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { message = "任务描述不能为空。" });
        }

        if (request.ProgressPercent is < 0 or > 100)
        {
            return BadRequest(new { message = "执行进程必须介于 0 到 100 之间。" });
        }

        var normalizedStatusCode = NormalizeOptionCode(StatusOptions, request.StatusCode, "PENDING");
        var normalizedTaskTypeCode = NormalizeOptionCode(TaskTypeOptions, request.TaskTypeCode, "TASK");
        var normalizedPriorityCode = NormalizeOptionCode(PriorityOptions, request.PriorityCode, "NORMAL");
        var operatorDisplayName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("erp:account") ?? "System";

        var taskFollowUp = new TaskFollowUp
        {
            Id = request.Id ?? 0,
            FeatureCode = request.FeatureCode.Trim(),
            EntityId = request.EntityId,
            TaskCode = request.TaskCode?.Trim() ?? string.Empty,
            ArchivePath = request.ArchivePath?.Trim(),
            StatusCode = normalizedStatusCode,
            PlannedDate = request.PlannedDate,
            TaskTypeCode = normalizedTaskTypeCode,
            ExecutorName = request.ExecutorName?.Trim(),
            Description = request.Description.Trim(),
            PriorityCode = normalizedPriorityCode,
            ProgressPercent = request.ProgressPercent,
            CompletedDate = request.CompletedDate,
            ProjectCode = request.ProjectCode?.Trim(),
            InitiatorName = string.IsNullOrWhiteSpace(request.InitiatorName) ? operatorDisplayName : request.InitiatorName.Trim(),
            CreatedBy = operatorDisplayName,
            UpdatedBy = operatorDisplayName
        };

        if (normalizedStatusCode == "COMPLETED" && taskFollowUp.CompletedDate == null)
        {
            taskFollowUp.CompletedDate = DateTime.Today;
        }

        var savedId = await _hrRepository.SaveTaskFollowUpAsync(taskFollowUp);
        if (savedId <= 0)
        {
            return BadRequest(new { message = "保存失败，请确认记录仍然存在后重试。" });
        }

        return Ok(new { id = savedId, message = request.Id > 0 ? "任务跟进已更新。" : "任务跟进已新增。" });
    }

    /// <summary>
    /// 删除任务跟进记录（支持当前功能与所属个体下的批量删除）。
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteTaskFollowUpsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FeatureCode) || request.EntityId <= 0)
        {
            return BadRequest(new { message = "功能编码和所属个体ID不能为空。" });
        }

        var deletedCount = await _hrRepository.DeleteTaskFollowUpsAsync(
            request.FeatureCode.Trim(),
            request.EntityId,
            request.Ids ?? []);

        return Ok(new { deletedCount, message = deletedCount > 0 ? "任务跟进已删除。" : "未找到可删除的任务跟进记录。" });
    }

    /// <summary>
    /// 解析选项显示名（根据选项编码返回界面标签）。
    /// </summary>
    private static string ResolveOptionLabel(IReadOnlyList<TaskFollowUpOptionItem> options, string? code)
        => options.FirstOrDefault(item => string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase))?.Label ?? string.Empty;

    /// <summary>
    /// 归一化选项编码（非法编码回退为默认值）。
    /// </summary>
    private static string NormalizeOptionCode(IReadOnlyList<TaskFollowUpOptionItem> options, string? code, string defaultCode)
        => options.Any(item => string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase))
            ? code!.Trim().ToUpperInvariant()
            : defaultCode;
}

/// <summary>
/// 任务跟进选项项（用于状态、类型、优先级下拉）。
/// </summary>
public class TaskFollowUpOptionItem
{
    /// <summary>
    /// 选项编码（存储到任务跟进表中的固定编码）。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 选项名称（界面展示名称）。
    /// </summary>
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// 保存任务跟进请求体（承载新增或编辑时提交的任务字段）。
/// </summary>
public class SaveTaskFollowUpRequest
{
    /// <summary>
    /// 任务跟进ID（为空表示新增，非空表示编辑）。
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// 功能编码（区分任务属于哪个业务功能）。
    /// </summary>
    public string FeatureCode { get; set; } = string.Empty;

    /// <summary>
    /// 所属个体ID（对应具体业务资料主键）。
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// 任务编号（为空时由系统自动生成）。
    /// </summary>
    public string? TaskCode { get; set; }

    /// <summary>
    /// 档案路径（附件或档案入口，当前可为空）。
    /// </summary>
    public string? ArchivePath { get; set; }

    /// <summary>
    /// 状态编码（固定选项编码）。
    /// </summary>
    public string? StatusCode { get; set; }

    /// <summary>
    /// 计划日期（预计执行或预计完成日期）。
    /// </summary>
    public DateTime? PlannedDate { get; set; }

    /// <summary>
    /// 类型编码（固定选项编码）。
    /// </summary>
    public string? TaskTypeCode { get; set; }

    /// <summary>
    /// 执行人（任务执行人姓名）。
    /// </summary>
    public string? ExecutorName { get; set; }

    /// <summary>
    /// 任务描述（任务内容说明）。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 优先级编码（固定选项编码）。
    /// </summary>
    public string? PriorityCode { get; set; }

    /// <summary>
    /// 执行进程（0-100 百分比整数）。
    /// </summary>
    public int ProgressPercent { get; set; }

    /// <summary>
    /// 完成日期（任务完成时间）。
    /// </summary>
    public DateTime? CompletedDate { get; set; }

    /// <summary>
    /// 项目编号（关联项目或外部单号）。
    /// </summary>
    public string? ProjectCode { get; set; }

    /// <summary>
    /// 发起人（任务指派发起人姓名）。
    /// </summary>
    public string? InitiatorName { get; set; }
}

/// <summary>
/// 删除任务跟进请求体（承载当前功能和所属个体下的选中记录ID）。
/// </summary>
public class DeleteTaskFollowUpsRequest
{
    /// <summary>
    /// 功能编码（区分任务属于哪个业务功能）。
    /// </summary>
    public string FeatureCode { get; set; } = string.Empty;

    /// <summary>
    /// 所属个体ID（对应具体业务资料主键）。
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// 待删除ID列表（仅删除这些选中的任务跟进记录）。
    /// </summary>
    public List<int>? Ids { get; set; }
}
