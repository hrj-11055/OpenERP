/*
 * File: OpenERP.Transport/Models/Entities/Vehicle.cs
 * Description: Domain entity definition for Vehicle in the OpenERP.Transport module.
 */

using System.ComponentModel.DataAnnotations;

namespace OpenERP.Transport.Models.Entities
{
    public class Vehicle : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? LicensePlate { get; set; }

        [StringLength(50)]
        public string? Type { get; set; } // Truck, Van, etc.

        [Range(0, 100000)]
        public decimal Capacity { get; set; }
    }
}
