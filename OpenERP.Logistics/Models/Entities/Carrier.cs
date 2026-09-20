/*
 * File: OpenERP.Logistics/Models/Entities/Carrier.cs
 * Description: Domain entity definition for Carrier in the OpenERP.Logistics module.
 */

using System.ComponentModel.DataAnnotations;

namespace OpenERP.Logistics.Models.Entities
{
    public class Carrier : BaseEntity
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ContactName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }
    }
}
