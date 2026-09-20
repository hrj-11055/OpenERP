/*
 * File: OpenERP.Sales/Models/Entities/Customer.cs
 * Description: Domain entity definition for Customer in the OpenERP.Sales module.
 */

using System.ComponentModel.DataAnnotations;

namespace OpenERP.Sales.Models.Entities
{
    public class Customer : BaseEntity
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
