namespace OpenERP.Web.Areas.Logistics.ViewModels.MaterialItems
{
    /// <summary>
    /// 共享资料列表页模型（包含主列表与底部关联记录页签）。
    /// </summary>
    public class MaterialItemIndexViewModel
    {
        /// <summary>
        /// 资料类别（产品、材料或辅料）。
        /// </summary>
        public string Category { get; set; } = "产品";

        /// <summary>
        /// 页面标题（当前资料类别显示名称）。
        /// </summary>
        public string PageTitle { get; set; } = "产品资料";

        /// <summary>
        /// 查询范围（全部、产品编号、产品名称等）。
        /// </summary>
        public string Scope { get; set; } = "全部";

        /// <summary>
        /// 查询关键字（列表筛选文本）。
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 当前选中的共享资料ID（用于底部关联页签）。
        /// </summary>
        public int? SelectedItemId { get; set; }

        /// <summary>
        /// 当前选中的共享资料名称（底部关联记录标题）。
        /// </summary>
        public string SelectedItemName { get; set; } = "未选择产品";

        /// <summary>
        /// 主列表资料集合。
        /// </summary>
        public List<MaterialItemListItemViewModel> Items { get; set; } = [];

        /// <summary>
        /// 底部关联记录页签集合（销售、采购、入库、出库、维修等）。
        /// </summary>
        public List<MaterialItemRelatedRecordViewModel> RelatedRecords { get; set; } = [];
    }
}
