/*
 * File: OpenERP.Sales/Models/Entities/SalesOrder.cs
 * Description: Domain entity definition for SalesOrder in the OpenERP.Sales module.
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.Sales.Models.Entities
{
    public class SalesOrder : BaseEntity
    {
        [Required]
        [StringLength(30)]
        public string OrderNumber { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.Today;

        public int? CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Draft";

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0m;
    }
}
