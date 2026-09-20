/*
 * File: OpenERP.Production/Models/Entities/ProductionOrderItem.cs
 * Description: Domain entity definition for ProductionOrderItem in the OpenERP.Production module.
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.Production.Models.Entities
{
    public class ProductionOrderItem : BaseEntity
    {
        [Required]
        [StringLength(150)]
        public string ProductName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        public int ProductionOrderId { get; set; }

        [ForeignKey("ProductionOrderId")]
        public virtual ProductionOrder? ProductionOrder { get; set; }
    }
}
