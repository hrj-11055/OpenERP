namespace OpenERP.Web.Areas.Logistics.ViewModels.WorkshopWarehouses
{
    /// <summary>
    /// 车间仓库资料列表页模型（包含仓库主表列表和所选仓库的库位列表）。
    /// </summary>
    public class WorkshopWarehouseIndexViewModel
    {
        /// <summary>
        /// 查询范围（全部、仓库编号、仓库名称、负责人、备注）。
        /// </summary>
        public string Scope { get; set; } = "全部";

        /// <summary>
        /// 关键字（用于过滤仓库列表）。
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 当前选中的仓库ID（用于显示下方库位记录）。
        /// </summary>
        public int? SelectedWarehouseId { get; set; }

        /// <summary>
        /// 仓库主表记录集合。
        /// </summary>
        public List<WorkshopWarehouseListItemViewModel> Warehouses { get; set; } = [];

        /// <summary>
        /// 所选仓库的库位记录集合。
        /// </summary>
        public List<WarehouseLocationListItemViewModel> Locations { get; set; } = [];

        /// <summary>
        /// 当前选中的仓库显示名称。
        /// </summary>
        public string SelectedWarehouseName { get; set; } = "未选择仓库";
    }

    /// <summary>
    /// 仓库列表行模型（对应车间仓库资料主表的一行）。
    /// </summary>
    public class WorkshopWarehouseListItemViewModel
    {
        /// <summary>
        /// 仓库ID（对应车间仓库资料实体）。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 序号（当前列表显示顺序）。
        /// </summary>
        public int SequenceNo { get; set; }

        /// <summary>
        /// 仓库编号（业务唯一编号）。
        /// </summary>
        public string WarehouseCode { get; set; } = string.Empty;

        /// <summary>
        /// 仓库名称（仓库显示名称）。
        /// </summary>
        public string WarehouseName { get; set; } = string.Empty;

        /// <summary>
        /// 仓库状态（例如正常、停用）。
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 仓库类型（例如原料仓库、冷库）。
        /// </summary>
        public string WarehouseType { get; set; } = string.Empty;

        /// <summary>
        /// 负责人（仓库日常管理责任人）。
        /// </summary>
        public string Manager { get; set; } = string.Empty;

        /// <summary>
        /// 地区（国家/城市/县区的合并显示值）。
        /// </summary>
        public string Region { get; set; } = string.Empty;

        /// <summary>
        /// 地址（仓库详细地址）。
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 电话（仓库联系电话）。
        /// </summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// 传真（仓库传真号码）。
        /// </summary>
        public string Fax { get; set; } = string.Empty;

        /// <summary>
        /// 电邮（仓库业务联系邮箱）。
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 备注（仓库补充说明）。
        /// </summary>
        public string Remarks { get; set; } = string.Empty;

        /// <summary>
        /// 最后修改人（来自审计字段）。
        /// </summary>
        public string LastModifiedBy { get; set; } = string.Empty;

        /// <summary>
        /// 最后修改时间（来自审计字段）。
        /// </summary>
        public string LastModifiedAt { get; set; } = string.Empty;
    }

    /// <summary>
    /// 库位列表行模型（对应仓库下方库位记录的一行）。
    /// </summary>
    public class WarehouseLocationListItemViewModel
    {
        /// <summary>
        /// 库位ID（对应库位资料实体）。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 序号（当前列表显示顺序）。
        /// </summary>
        public int SequenceNo { get; set; }

        /// <summary>
        /// 库位编号（同一仓库内唯一）。
        /// </summary>
        public string LocationCode { get; set; } = string.Empty;

        /// <summary>
        /// 库位名称（库位显示名称）。
        /// </summary>
        public string LocationName { get; set; } = string.Empty;

        /// <summary>
        /// 库位状态（例如正常、停用）。
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 库位级别（例如ABC）。
        /// </summary>
        public string LocationLevel { get; set; } = string.Empty;

        /// <summary>
        /// 库区（仓库内的分区）。
        /// </summary>
        public string Zone { get; set; } = string.Empty;

        /// <summary>
        /// 存储环境（例如常温、冷库）。
        /// </summary>
        public string StorageEnvironment { get; set; } = string.Empty;

        /// <summary>
        /// 用途（例如暂收区、存储区、发货区）。
        /// </summary>
        public string Usage { get; set; } = string.Empty;

        /// <summary>
        /// 最大装载重量KG。
        /// </summary>
        public decimal MaxLoadKg { get; set; }

        /// <summary>
        /// 最大装载空间CBM。
        /// </summary>
        public decimal MaxVolumeCbm { get; set; }

        /// <summary>
        /// 备注（库位补充说明）。
        /// </summary>
        public string Remarks { get; set; } = string.Empty;
    }
}
