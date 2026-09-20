/*
 * File: OpenERP.Production/Models/Entities/WorkCenter.cs
 * Description: Domain entity definition for WorkCenter in the OpenERP.Production module.
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.Production.Models.Entities
{
    public class WorkCenter : BaseEntity
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Code { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Capacity { get; set; } = 0m;
    }
}
