using System.ComponentModel.DataAnnotations;

namespace OpenERP.Web.Areas.Logistics.ViewModels.WorkshopWarehouses
{
    /// <summary>
    /// 库位资料编辑输入模型（用于车间仓库详情页提交库位明细）。
    /// </summary>
    public class WarehouseLocationInputModel
    {
        /// <summary>
        /// 库位ID（已有库位编辑时使用，新建库位为空）。
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// 库位编号（同一仓库内唯一）。
        /// </summary>
        [Required(ErrorMessage = "请输入库位编号")]
        [StringLength(30, ErrorMessage = "库位编号不能超过30个字符")]
        public string LocationCode { get; set; } = string.Empty;

        /// <summary>
        /// 库位名称（库位显示名称）。
        /// </summary>
        [Required(ErrorMessage = "请输入库位名称")]
        [StringLength(150, ErrorMessage = "库位名称不能超过150个字符")]
        public string LocationName { get; set; } = string.Empty;

        /// <summary>
        /// 库位状态（例如正常、停用）。
        /// </summary>
        [Required(ErrorMessage = "请选择库位状态")]
        [StringLength(20, ErrorMessage = "库位状态不能超过20个字符")]
        public string Status { get; set; } = "正常";

        /// <summary>
        /// 库位级别（例如ABC）。
        /// </summary>
        [StringLength(50, ErrorMessage = "库位级别不能超过50个字符")]
        public string? LocationLevel { get; set; }

        /// <summary>
        /// 库区（仓库内的分区）。
        /// </summary>
        [StringLength(50, ErrorMessage = "库区不能超过50个字符")]
        public string? Zone { get; set; }

        /// <summary>
        /// 存储环境（例如常温、冷库）。
        /// </summary>
        [StringLength(50, ErrorMessage = "存储环境不能超过50个字符")]
        public string? StorageEnvironment { get; set; }

        /// <summary>
        /// 用途（例如暂收区、存储区、发货区）。
        /// </summary>
        [StringLength(50, ErrorMessage = "用途不能超过50个字符")]
        public string? Usage { get; set; }

        /// <summary>
        /// 最大装载重量KG。
        /// </summary>
        [Range(0, 999999999, ErrorMessage = "最大装载重量不能小于0")]
        public decimal MaxLoadKg { get; set; }

        /// <summary>
        /// 最大装载空间CBM。
        /// </summary>
        [Range(0, 999999999, ErrorMessage = "最大装载空间不能小于0")]
        public decimal MaxVolumeCbm { get; set; }

        /// <summary>
        /// 备注（库位补充说明）。
        /// </summary>
        [StringLength(500, ErrorMessage = "备注不能超过500个字符")]
        public string? Remarks { get; set; }

        /// <summary>
        /// 删除标记（详情页移除库位时提交，保存时软删除）。
        /// </summary>
        public bool IsDeleted { get; set; }
    }
}
