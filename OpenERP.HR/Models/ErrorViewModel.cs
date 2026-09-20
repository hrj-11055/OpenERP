/*
 * File: OpenERP.HR/Models/ErrorViewModel.cs
 * Description: View model used by the error page in OpenERP.HR.
 */

namespace OpenERP.HR.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
