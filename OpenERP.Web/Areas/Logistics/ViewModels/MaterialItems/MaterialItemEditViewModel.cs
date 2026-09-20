using System.ComponentModel.DataAnnotations;

namespace OpenERP.Web.Areas.Logistics.ViewModels.MaterialItems
{
    /// <summary>
    /// 共享资料详情编辑模型（按产品资料栏位定义，材料、辅料等入口复用）。
    /// </summary>
    public class MaterialItemEditViewModel
    {
        /// <summary>
        /// 共享资料ID（对应 BD_ItemMaster 主键；新增时为空）。
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// 资料类别（产品、材料、辅料、资产资料、办公用品）。
        /// </summary>
        public string Category { get; set; } = "产品";

        /// <summary>
        /// 资料编号（产品或物料业务唯一编号）。
        /// </summary>
        [Required(ErrorMessage = "请输入产品编号。")]
        [StringLength(50, ErrorMessage = "产品编号不能超过 50 个字符。")]
        public string ItemCode { get; set; } = string.Empty;

        /// <summary>
        /// 条形码（产品主条码）。
        /// </summary>
        [StringLength(80, ErrorMessage = "条形码不能超过 80 个字符。")]
        public string? Barcode { get; set; }

        /// <summary>
        /// 产品名称（产品正式名称）。
        /// </summary>
        [Required(ErrorMessage = "请输入产品名称。")]
        [StringLength(200, ErrorMessage = "产品名称不能超过 200 个字符。")]
        public string ItemName { get; set; } = string.Empty;

        /// <summary>
        /// 状态（正常、停用等）。
        /// </summary>
        [Required(ErrorMessage = "请选择状态。")]
        public string Status { get; set; } = "正常";

        /// <summary>
        /// 类型（产品分类，来自基础数据字典）。
        /// </summary>
        [Required(ErrorMessage = "请选择类型。")]
        public string ItemType { get; set; } = "电子";

        /// <summary>
        /// 品牌（产品品牌）。
        /// </summary>
        public string? Brand { get; set; } = "华为";

        /// <summary>
        /// 型号（产品型号）。
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// 规格（产品规格）。
        /// </summary>
        public string? Specification { get; set; }

        /// <summary>
        /// 产地（产品来源地区）。
        /// </summary>
        public string? Origin { get; set; } = "中国";

        /// <summary>
        /// 基本单位（库存与单据基准单位）。
        /// </summary>
        [Required(ErrorMessage = "请选择基本单位。")]
        public string BaseUnit { get; set; } = "部";

        /// <summary>
        /// 进货单位（采购入库使用单位）。
        /// </summary>
        public string PurchaseUnit { get; set; } = "箱";

        /// <summary>
        /// 库存单位（库存余额展示单位）。
        /// </summary>
        public string InventoryUnit { get; set; } = "部";

        /// <summary>
        /// 销售单位（销售单据使用单位）。
        /// </summary>
        public string SalesUnit { get; set; } = "部";

        /// <summary>
        /// 进货单位换算数量（一个进货单位等于多少基本单位）。
        /// </summary>
        public decimal PurchaseUnitRate { get; set; } = 10M;

        /// <summary>
        /// 库存单位换算数量（一个库存单位等于多少基本单位）。
        /// </summary>
        public decimal InventoryUnitRate { get; set; } = 1M;

        /// <summary>
        /// 销售单位换算数量（一个销售单位等于多少基本单位）。
        /// </summary>
        public decimal SalesUnitRate { get; set; } = 1M;

        /// <summary>
        /// 当前库存数量（只读展示）。
        /// </summary>
        public decimal InventoryQuantity { get; set; } = 300M;

        /// <summary>
        /// 库存容量上限（只读展示）。
        /// </summary>
        public decimal InventoryCapacity { get; set; } = 500M;

        /// <summary>
        /// 首选库位（默认仓库和库位）。
        /// </summary>
        public string? PrimaryLocation { get; set; } = "广州仓库/AB区/A01-01";

        /// <summary>
        /// 当前成本价（库存或财务成本参考价）。
        /// </summary>
        public decimal CurrentCost { get; set; } = 900M;

        /// <summary>
        /// 建议售价（产品报价参考价）。
        /// </summary>
        public decimal SuggestedPrice { get; set; } = 1500M;

        /// <summary>
        /// 产品形态（成品或配件）。
        /// </summary>
        public string ProductForm { get; set; } = "成品";

        /// <summary>
        /// 详细描述（产品中文描述）。
        /// </summary>
        public string? DetailDescription { get; set; }

        /// <summary>
        /// 外文名称（产品外文名称）。
        /// </summary>
        public string? ForeignName { get; set; }

        /// <summary>
        /// 外文描述（产品外文描述）。
        /// </summary>
        public string? ForeignDescription { get; set; }

        /// <summary>
        /// 供应商货号（供应商侧产品编号）。
        /// </summary>
        public string? SupplierItemCode { get; set; }

        /// <summary>
        /// 海关代码（报关商品编码）。
        /// </summary>
        public string? CustomsCode { get; set; }

        /// <summary>
        /// 国际条形码（全球贸易条码）。
        /// </summary>
        public string? InternationalBarcode { get; set; }

        /// <summary>
        /// 材质（产品主要材质）。
        /// </summary>
        public string? MaterialTexture { get; set; }

        /// <summary>
        /// 特性（产品功能或卖点）。
        /// </summary>
        public string? Feature { get; set; }

        /// <summary>
        /// 自定义栏位1（用户预留字段）。
        /// </summary>
        public string? CustomField1 { get; set; }

        /// <summary>
        /// 自定义栏位2（用户预留字段）。
        /// </summary>
        public string? CustomField2 { get; set; }

        /// <summary>
        /// 自定义栏位3（用户预留日期字段）。
        /// </summary>
        public DateOnly? CustomDate3 { get; set; }

        /// <summary>
        /// 自定义栏位4（用户预留字段）。
        /// </summary>
        public string? CustomField4 { get; set; }

        /// <summary>
        /// 自定义栏位5（用户预留字段）。
        /// </summary>
        public string? CustomField5 { get; set; }

        /// <summary>
        /// 自定义栏位6（用户预留字段）。
        /// </summary>
        public string? CustomField6 { get; set; }

        /// <summary>
        /// 自定义栏位7（用户预留字段）。
        /// </summary>
        public string? CustomField7 { get; set; }

        /// <summary>
        /// 自定义栏位8（用户预留字段）。
        /// </summary>
        public string? CustomField8 { get; set; }

        /// <summary>
        /// 自定义栏位9（用户预留字段）。
        /// </summary>
        public string? CustomField9 { get; set; }

        /// <summary>
        /// 网址（产品官网或资料链接）。
        /// </summary>
        public string? Website { get; set; }

        /// <summary>
        /// 备注（产品资料补充说明）。
        /// </summary>
        public string? Remarks { get; set; }

        /// <summary>
        /// 存档路径（旧字段保留；实际附件使用通用文档管理）。
        /// </summary>
        public string? ArchivePath { get; set; }

        /// <summary>
        /// 单个包装数量（一个单品包装包含的基本单位数量）。
        /// </summary>
        public decimal SinglePackageQuantity { get; set; } = 1M;

        /// <summary>
        /// 小包包装数量（小包包含的基本单位数量）。
        /// </summary>
        public decimal InnerPackageQuantity { get; set; } = 5M;

        /// <summary>
        /// 箱装包装数量（整箱包含的基本单位数量）。
        /// </summary>
        public decimal CartonPackageQuantity { get; set; } = 20M;

        /// <summary>
        /// 其他包装数量（自定义包装数量）。
        /// </summary>
        public decimal? OtherPackageQuantity { get; set; }

        /// <summary>
        /// 单个包装长宽高（厘米）。
        /// </summary>
        public string? SinglePackageSize { get; set; } = "2 x 2 x 2";

        /// <summary>
        /// 小包包装长宽高（厘米）。
        /// </summary>
        public string? InnerPackageSize { get; set; } = "10 x 10 x 10";

        /// <summary>
        /// 箱装包装长宽高（厘米）。
        /// </summary>
        public string? CartonPackageSize { get; set; } = "20 x 20 x 20";

        /// <summary>
        /// 其他包装长宽高（厘米）。
        /// </summary>
        public string? OtherPackageSize { get; set; }

        /// <summary>
        /// 单个包装体积（CBM）。
        /// </summary>
        public decimal SingleCbm { get; set; } = 2M;

        /// <summary>
        /// 小包包装体积（CBM）。
        /// </summary>
        public decimal InnerCbm { get; set; } = 10M;

        /// <summary>
        /// 箱装包装体积（CBM）。
        /// </summary>
        public decimal CartonCbm { get; set; } = 20M;

        /// <summary>
        /// 其他包装体积（CBM）。
        /// </summary>
        public decimal? OtherCbm { get; set; }

        /// <summary>
        /// 单个包装净重（千克）。
        /// </summary>
        public decimal SingleNetWeight { get; set; } = 2M;

        /// <summary>
        /// 小包包装净重（千克）。
        /// </summary>
        public decimal InnerNetWeight { get; set; } = 10M;

        /// <summary>
        /// 箱装包装净重（千克）。
        /// </summary>
        public decimal CartonNetWeight { get; set; } = 20M;

        /// <summary>
        /// 其他包装净重（千克）。
        /// </summary>
        public decimal? OtherNetWeight { get; set; }

        /// <summary>
        /// 单个包装毛重（千克）。
        /// </summary>
        public decimal SingleGrossWeight { get; set; } = 2M;

        /// <summary>
        /// 小包包装毛重（千克）。
        /// </summary>
        public decimal InnerGrossWeight { get; set; } = 10M;

        /// <summary>
        /// 箱装包装毛重（千克）。
        /// </summary>
        public decimal CartonGrossWeight { get; set; } = 20M;

        /// <summary>
        /// 其他包装毛重（千克）。
        /// </summary>
        public decimal? OtherGrossWeight { get; set; }

        /// <summary>
        /// 唛头描述（包装外箱标识）。
        /// </summary>
        public string? ShippingMark { get; set; }

        /// <summary>
        /// 启用序列号（单件序列追踪）。
        /// </summary>
        public bool EnableSerialNumber { get; set; }

        /// <summary>
        /// 启用批次（批次追踪）。
        /// </summary>
        public bool EnableBatch { get; set; } = true;

        /// <summary>
        /// 启用保质期（有效期追踪）。
        /// </summary>
        public bool EnableShelfLife { get; set; } = true;

        /// <summary>
        /// 保质期天数（启用保质期时使用）。
        /// </summary>
        public int ShelfLifeDays { get; set; } = 180;

        /// <summary>
        /// 启用保养期（维护保养周期追踪）。
        /// </summary>
        public bool EnableMaintenancePeriod { get; set; }

        /// <summary>
        /// 保养期天数（启用保养期时使用）。
        /// </summary>
        public int MaintenancePeriodDays { get; set; } = 365;

        /// <summary>
        /// BOM 明细行集合。
        /// </summary>
        public List<MaterialItemBomLineInputModel> BomLines { get; set; } = [];

        /// <summary>
        /// 售价设置明细行集合。
        /// </summary>
        public List<MaterialItemPriceLineInputModel> PriceLines { get; set; } = [];

    }
}
