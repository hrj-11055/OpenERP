/*
 * File: OpenERP.HR/Models/Entities/Position.cs
 * Description: Domain entity definition for Position in the OpenERP.HR module.
 */

using System.ComponentModel.DataAnnotations;

namespace OpenERP.HR.Models.Entities
{
    public class Position : BaseEntity
    {
        /// <summary>
        /// 职位名称（岗位主数据名称，用于员工档案和组织岗位维护）。
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 职位说明（岗位职责或补充描述，可为空）。
        /// </summary>
        [StringLength(200)]
        public string? Description { get; set; }
    }
}
