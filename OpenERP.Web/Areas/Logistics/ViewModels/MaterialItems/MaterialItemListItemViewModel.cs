namespace OpenERP.Web.Areas.Logistics.ViewModels.MaterialItems
{
    /// <summary>
    /// 共享资料列表行模型（用于产品、材料、辅料列表展示）。
    /// </summary>
    public class MaterialItemListItemViewModel
    {
        /// <summary>
        /// 共享资料ID（对应 BD_ItemMaster 主表）。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 序号（当前列表显示顺序）。
        /// </summary>
        public int SequenceNo { get; set; }

        /// <summary>
        /// 资料编号（产品或物料业务编号）。
        /// </summary>
        public string ItemCode { get; set; } = string.Empty;

        /// <summary>
        /// 资料名称（产品或物料名称）。
        /// </summary>
        public string ItemName { get; set; } = string.Empty;

        /// <summary>
        /// 状态（正常、停用等）。
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 资料档案按钮文案（通用文档管理入口）。
        /// </summary>
        public string ArchiveLabel { get; set; } = "档案";

        /// <summary>
        /// 产品描述（列表短描述）。
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 类型（产品分类或物料分类）。
        /// </summary>
        public string ItemType { get; set; } = string.Empty;

        /// <summary>
        /// 品牌（产品品牌）。
        /// </summary>
        public string Brand { get; set; } = string.Empty;

        /// <summary>
        /// 规格/型号（产品规格和型号合并展示）。
        /// </summary>
        public string SpecificationModel { get; set; } = string.Empty;

        /// <summary>
        /// 产地（产品来源地区）。
        /// </summary>
        public string Origin { get; set; } = string.Empty;

        /// <summary>
        /// 单位（基本单位）。
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// 库存（当前库存数量）。
        /// </summary>
        public string Inventory { get; set; } = string.Empty;

        /// <summary>
        /// 备注（资料补充说明）。
        /// </summary>
        public string Remarks { get; set; } = string.Empty;

        /// <summary>
        /// 最后修改人（更新人或创建人）。
        /// </summary>
        public string LastModifiedBy { get; set; } = string.Empty;

        /// <summary>
        /// 最后修改时间（更新或创建时间）。
        /// </summary>
        public string LastModifiedAt { get; set; } = string.Empty;
    }
}
