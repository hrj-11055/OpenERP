/*
 * File: OpenERP.Purchasing/Models/Entities/PurchaseOrder.cs
 * Description: Domain entity definition for PurchaseOrder in the OpenERP.Purchasing module.
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.Purchasing.Models.Entities
{
    public class PurchaseOrder : BaseEntity
    {
        [Required]
        [StringLength(30)]
        public string OrderNumber { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.Today;

        public int? SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public virtual Supplier? Supplier { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Draft";

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0m;
    }
}
