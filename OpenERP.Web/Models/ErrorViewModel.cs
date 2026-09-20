/*
 * File: OpenERP.Web/Models/ErrorViewModel.cs
 * Description: View model used by the error page in OpenERP.Web.
 */

namespace OpenERP.Web.Models;

/// <summary>
/// 错误页视图模型。
/// </summary>
public class ErrorViewModel
{
    /// <summary>
    /// 请求跟踪ID（用于定位本次请求链路）。
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// 是否显示请求跟踪ID（有值时显示）。
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
