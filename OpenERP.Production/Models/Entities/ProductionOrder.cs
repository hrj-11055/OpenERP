/*
 * File: OpenERP.Production/Models/Entities/ProductionOrder.cs
 * Description: Domain entity definition for ProductionOrder in the OpenERP.Production module.
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.Production.Models.Entities
{
    public class ProductionOrder : BaseEntity
    {
        [Required]
        [StringLength(30)]
        public string OrderNumber { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.Today;

        public int? WorkCenterId { get; set; }

        [ForeignKey("WorkCenterId")]
        public virtual WorkCenter? WorkCenter { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Planned";

        [Required]
        [StringLength(150)]
        public string ProductName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PlannedQuantity { get; set; } = 0m;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalQuantity { get; set; } = 0m;
    }
}
