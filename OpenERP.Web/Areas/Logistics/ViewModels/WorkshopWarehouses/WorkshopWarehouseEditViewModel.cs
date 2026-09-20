using System.ComponentModel.DataAnnotations;

namespace OpenERP.Web.Areas.Logistics.ViewModels.WorkshopWarehouses
{
    /// <summary>
    /// 车间仓库资料详情页编辑模型（包含主表仓库资料和从表库位资料）。
    /// </summary>
    public class WorkshopWarehouseEditViewModel
    {
        /// <summary>
        /// 仓库ID（新增时为空，编辑时对应车间仓库资料实体）。
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// 仓库编号（业务唯一编号）。
        /// </summary>
        [Required(ErrorMessage = "请输入仓库编号")]
        [StringLength(30, ErrorMessage = "仓库编号不能超过30个字符")]
        public string WarehouseCode { get; set; } = string.Empty;

        /// <summary>
        /// 仓库名称（仓库显示名称）。
        /// </summary>
        [Required(ErrorMessage = "请输入仓库名称")]
        [StringLength(150, ErrorMessage = "仓库名称不能超过150个字符")]
        public string WarehouseName { get; set; } = string.Empty;

        /// <summary>
        /// 仓库状态（例如正常、停用）。
        /// </summary>
        [Required(ErrorMessage = "请选择状态")]
        [StringLength(20, ErrorMessage = "状态不能超过20个字符")]
        public string Status { get; set; } = "正常";

        /// <summary>
        /// 仓库类型（例如原料仓库、冷库）。
        /// </summary>
        [StringLength(40, ErrorMessage = "类型不能超过40个字符")]
        public string? WarehouseType { get; set; } = "原料仓库";

        /// <summary>
        /// 负责人（仓库日常管理责任人）。
        /// </summary>
        [StringLength(80, ErrorMessage = "负责人不能超过80个字符")]
        public string? Manager { get; set; }

        /// <summary>
        /// 国家/地区（地址行政区划第一层）。
        /// </summary>
        [StringLength(80, ErrorMessage = "国家/地区不能超过80个字符")]
        public string? CountryRegion { get; set; } = "中国华南";

        /// <summary>
        /// 城市（地址行政区划第二层）。
        /// </summary>
        [StringLength(80, ErrorMessage = "城市不能超过80个字符")]
        public string? City { get; set; } = "广州市";

        /// <summary>
        /// 县/区（地址行政区划第三层）。
        /// </summary>
        [StringLength(80, ErrorMessage = "县/区不能超过80个字符")]
        public string? District { get; set; } = "天河区";

        /// <summary>
        /// 仓库地址（可填写多行）。
        /// </summary>
        [StringLength(500, ErrorMessage = "地址不能超过500个字符")]
        public string? Address { get; set; }

        /// <summary>
        /// 联系电话（仓库对外联系电话）。
        /// </summary>
        [StringLength(50, ErrorMessage = "电话不能超过50个字符")]
        public string? Phone { get; set; }

        /// <summary>
        /// 传真号码（仓库对外传真）。
        /// </summary>
        [StringLength(50, ErrorMessage = "传真不能超过50个字符")]
        public string? Fax { get; set; }

        /// <summary>
        /// 电子邮箱（仓库业务联系邮箱）。
        /// </summary>
        [EmailAddress(ErrorMessage = "请输入有效的电邮地址")]
        [StringLength(120, ErrorMessage = "电邮不能超过120个字符")]
        public string? Email { get; set; }

        /// <summary>
        /// 备注（仓库补充说明）。
        /// </summary>
        [StringLength(1000, ErrorMessage = "备注不能超过1000个字符")]
        public string? Remarks { get; set; }

        /// <summary>
        /// 库位资料明细（当前仓库下的库位集合）。
        /// </summary>
        public List<WarehouseLocationInputModel> Locations { get; set; } = [];
    }
}
