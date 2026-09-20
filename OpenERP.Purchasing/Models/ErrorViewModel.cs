/*
 * File: OpenERP.Purchasing/Models/ErrorViewModel.cs
 * Description: View model used by the error page in OpenERP.Purchasing.
 */

namespace OpenERP.Purchasing.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
