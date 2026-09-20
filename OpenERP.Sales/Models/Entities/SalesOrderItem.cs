/*
 * File: OpenERP.Sales/Models/Entities/SalesOrderItem.cs
 * Description: Domain entity definition for SalesOrderItem in the OpenERP.Sales module.
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.Sales.Models.Entities
{
    public class SalesOrderItem : BaseEntity
    {
        [Required]
        [StringLength(150)]
        public string ProductName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public int SalesOrderId { get; set; }

        [ForeignKey("SalesOrderId")]
        public virtual SalesOrder? SalesOrder { get; set; }
    }
}
