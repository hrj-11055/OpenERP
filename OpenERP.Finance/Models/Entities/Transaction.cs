/*
 * File: OpenERP.Finance/Models/Entities/Transaction.cs
 * Description: Domain entity definition for Transaction in the OpenERP.Finance module.
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.Finance.Models.Entities
{
    public class Transaction : BaseEntity
    {
        [Required]
        [StringLength(30)]
        public string DocNumber { get; set; } = string.Empty;

        public DateTime DocDate { get; set; } = DateTime.Today;

        public int? AccountId { get; set; }

        [ForeignKey("AccountId")]
        public virtual Account? Account { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Draft";
    }
}
