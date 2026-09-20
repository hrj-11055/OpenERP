/*
 * File: OpenERP.HR/Models/Entities/Department.cs
 * Description: Domain entity definition for Department in the OpenERP.HR module.
 */

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenERP.HR.Models.Entities
{
    /// <summary>
    /// 部门实体（对应组织架构中的部门主数据）。
    /// </summary>
    public class Department : BaseEntity
    {
        /// <summary>
        /// 部门名称（用于部门列表与员工所属部门展示）。
        /// </summary>
        [Display(Name = "Department_Name")]
        [Required(ErrorMessage = "Department_NameRequired")]
        [StringLength(100, ErrorMessage = "Department_NameMaxLength")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 部门描述（用于补充说明部门职责与范围）。
        /// </summary>
        [Display(Name = "Common_Description")]
        public string? Description { get; set; }

        /// <summary>
        /// 员工集合（当前部门下关联的员工记录）。
        /// </summary>
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
