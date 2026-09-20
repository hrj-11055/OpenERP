using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.Logistics.Models.Entities
{
    /// <summary>
    /// 库位资料从表（记录车间仓库内的细分存放位置）。
    /// </summary>
    public class WarehouseLocation : BaseEntity
    {
        /// <summary>
        /// 所属仓库ID（对应车间仓库资料主表）。
        /// </summary>
        public int WorkshopWarehouseId { get; set; }

        /// <summary>
        /// 所属仓库（库位归属的车间仓库）。
        /// </summary>
        [ForeignKey(nameof(WorkshopWarehouseId))]
        public virtual WorkshopWarehouse? WorkshopWarehouse { get; set; }

        /// <summary>
        /// 库位编号（同一仓库内唯一，用于库存定位）。
        /// </summary>
        [Required]
        [StringLength(30)]
        public string LocationCode { get; set; } = string.Empty;

        /// <summary>
        /// 库位名称（库位的中文显示名称）。
        /// </summary>
        [Required]
        [StringLength(150)]
        public string LocationName { get; set; } = string.Empty;

        /// <summary>
        /// 库位状态（例如正常、停用）。
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "正常";

        /// <summary>
        /// 库位级别（例如ABC，用于库位分类管理）。
        /// </summary>
        [StringLength(50)]
        public string? LocationLevel { get; set; }

        /// <summary>
        /// 库区（仓库内的分区，例如A区）。
        /// </summary>
        [StringLength(50)]
        public string? Zone { get; set; }

        /// <summary>
        /// 存储环境（例如常温、冷库）。
        /// </summary>
        [StringLength(50)]
        public string? StorageEnvironment { get; set; }

        /// <summary>
        /// 用途（例如暂收区、存储区、发货区）。
        /// </summary>
        [StringLength(50)]
        public string? Usage { get; set; }

        /// <summary>
        /// 最大装载重量KG（库位允许的最大承重）。
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal MaxLoadKg { get; set; }

        /// <summary>
        /// 最大装载空间CBM（库位允许的最大体积）。
        /// </summary>
        [Column(TypeName = "decimal(18,3)")]
        public decimal MaxVolumeCbm { get; set; }

        /// <summary>
        /// 备注（库位补充说明）。
        /// </summary>
        [StringLength(500)]
        public string? Remarks { get; set; }

        /// <summary>
        /// 排序号（同一仓库内库位显示顺序）。
        /// </summary>
        public int SortOrder { get; set; }
    }
}
