/*
 * File: OpenERP.Purchasing/Models/Entities/Supplier.cs
 * Description: Domain entity definition for Supplier in the OpenERP.Purchasing module.
 */

using System.ComponentModel.DataAnnotations;

namespace OpenERP.Purchasing.Models.Entities
{
    public class Supplier : BaseEntity
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
