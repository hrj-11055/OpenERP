/*
 * File: OpenERP.Finance/Models/Entities/Account.cs
 * Description: Domain entity definition for Account in the OpenERP.Finance module.
 */

using System.ComponentModel.DataAnnotations;

namespace OpenERP.Finance.Models.Entities
{
    public class Account : BaseEntity
    {
        [Required]
        [StringLength(30)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Type { get; set; } // Asset, Liability, Equity, Revenue, Expense
    }
}
