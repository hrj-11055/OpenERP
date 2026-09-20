/*
 * File: OpenERP.Asset/Models/Entities/BaseEntity.cs
 * Description: Domain entity definition for BaseEntity in the OpenERP.Asset module.
 */

using System;
using System.ComponentModel.DataAnnotations;

namespace OpenERP.Asset.Models.Entities
{
    public abstract class BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [StringLength(50)]
        public string? CreatedBy { get; set; }

        [StringLength(50)]
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
