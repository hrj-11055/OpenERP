/*
 * File: OpenERP.Logistics/Models/Entities/ShipmentItem.cs
 * Description: Domain entity definition for ShipmentItem in the OpenERP.Logistics module.
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.Logistics.Models.Entities
{
    public class ShipmentItem : BaseEntity
    {
        [Required]
        [StringLength(150)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Weight { get; set; }

        public int ShipmentId { get; set; }

        [ForeignKey("ShipmentId")]
        public virtual Shipment? Shipment { get; set; }
    }
}
