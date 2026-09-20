using System.ComponentModel.DataAnnotations;

namespace OpenERP.Logistics.Models.Entities
{
    /// <summary>
    /// 车间仓库资料主表（记录车间可用仓库及其联系、区域、状态信息）。
    /// </summary>
    public class WorkshopWarehouse : BaseEntity
    {
        /// <summary>
        /// 仓库编号（业务唯一编号，用于列表查询和单据引用）。
        /// </summary>
        [Required]
        [StringLength(30)]
        public string WarehouseCode { get; set; } = string.Empty;

        /// <summary>
        /// 仓库名称（仓库的中文显示名称）。
        /// </summary>
        [Required]
        [StringLength(150)]
        public string WarehouseName { get; set; } = string.Empty;

        /// <summary>
        /// 仓库状态（例如正常、停用）。
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "正常";

        /// <summary>
        /// 仓库类型（例如原料仓库、冷库、成品仓库）。
        /// </summary>
        [StringLength(40)]
        public string? WarehouseType { get; set; }

        /// <summary>
        /// 负责人（仓库日常管理责任人）。
        /// </summary>
        [StringLength(80)]
        public string? Manager { get; set; }

        /// <summary>
        /// 国家/地区（地址行政区划第一层）。
        /// </summary>
        [StringLength(80)]
        public string? CountryRegion { get; set; }

        /// <summary>
        /// 城市（地址行政区划第二层）。
        /// </summary>
        [StringLength(80)]
        public string? City { get; set; }

        /// <summary>
        /// 县/区（地址行政区划第三层）。
        /// </summary>
        [StringLength(80)]
        public string? District { get; set; }

        /// <summary>
        /// 仓库地址（可存放多行详细地址）。
        /// </summary>
        [StringLength(500)]
        public string? Address { get; set; }

        /// <summary>
        /// 联系电话（仓库对外联系电话）。
        /// </summary>
        [StringLength(50)]
        public string? Phone { get; set; }

        /// <summary>
        /// 传真号码（仓库对外传真）。
        /// </summary>
        [StringLength(50)]
        public string? Fax { get; set; }

        /// <summary>
        /// 电子邮箱（仓库业务联系邮箱）。
        /// </summary>
        [EmailAddress]
        [StringLength(120)]
        public string? Email { get; set; }

        /// <summary>
        /// 备注（仓库补充说明）。
        /// </summary>
        [StringLength(1000)]
        public string? Remarks { get; set; }

        /// <summary>
        /// 库位明细集合（当前仓库下的细分存储位置）。
        /// </summary>
        public virtual ICollection<WarehouseLocation> Locations { get; set; } = new List<WarehouseLocation>();
    }
}
