using System.ComponentModel.DataAnnotations;

namespace OpenERP.Logistics.Models.Entities
{
    /// <summary>
    /// 共享资料主表（产品、材料、辅料、资产资料、办公用品共用同一张资料表）。
    /// </summary>
    public class MaterialItem : BaseEntity
    {
        /// <summary>
        /// 资料类别（区分产品、材料、辅料、资产资料、办公用品等业务入口）。
        /// </summary>
        [Required]
        [StringLength(40)]
        public string ItemCategory { get; set; } = "产品";

        /// <summary>
        /// 资料编号（产品或物料业务唯一编号）。
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ItemCode { get; set; } = string.Empty;

        /// <summary>
        /// 条形码（产品主条码，来自产品包装或扫码管理）。
        /// </summary>
        [StringLength(80)]
        public string? Barcode { get; set; }

        /// <summary>
        /// 资料名称（产品、材料或办公用品的正式名称）。
        /// </summary>
        [Required]
        [StringLength(200)]
        public string ItemName { get; set; } = string.Empty;

        /// <summary>
        /// 状态（正常、停用等资料可用状态）。
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "正常";

        /// <summary>
        /// 类型（产品分类或资料类型，来自基础数据字典）。
        /// </summary>
        [StringLength(60)]
        public string? ItemType { get; set; } = "电子";

        /// <summary>
        /// 品牌（产品品牌或物料品牌）。
        /// </summary>
        [StringLength(100)]
        public string? Brand { get; set; }

        /// <summary>
        /// 型号（产品型号或物料型号）。
        /// </summary>
        [StringLength(100)]
        public string? Model { get; set; }

        /// <summary>
        /// 规格（产品规格或包装规格）。
        /// </summary>
        [StringLength(120)]
        public string? Specification { get; set; }

        /// <summary>
        /// 产地（产品或物料来源地区）。
        /// </summary>
        [StringLength(100)]
        public string? Origin { get; set; }

        /// <summary>
        /// 基本单位（库存与单据换算的基准单位）。
        /// </summary>
        [StringLength(30)]
        public string? BaseUnit { get; set; } = "部";

        /// <summary>
        /// 进货单位（采购入库使用的单位）。
        /// </summary>
        [StringLength(30)]
        public string? PurchaseUnit { get; set; } = "箱";

        /// <summary>
        /// 库存单位（库存余额展示和计算单位）。
        /// </summary>
        [StringLength(30)]
        public string? InventoryUnit { get; set; } = "部";

        /// <summary>
        /// 销售单位（销售订单和报价使用的单位）。
        /// </summary>
        [StringLength(30)]
        public string? SalesUnit { get; set; } = "部";

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
        /// 当前库存数量（库存模块汇总值，当前页面只读展示）。
        /// </summary>
        public decimal InventoryQuantity { get; set; }

        /// <summary>
        /// 库存容量上限（用于展示库存数量分母或安全容量）。
        /// </summary>
        public decimal InventoryCapacity { get; set; }

        /// <summary>
        /// 首选库位（默认仓库/库位路径，来自仓务管理库位资料）。
        /// </summary>
        [StringLength(200)]
        public string? PrimaryLocation { get; set; }

        /// <summary>
        /// 当前成本价（财务或库存成本计算后的参考成本）。
        /// </summary>
        public decimal CurrentCost { get; set; }

        /// <summary>
        /// 建议售价（产品销售报价默认参考价）。
        /// </summary>
        public decimal SuggestedPrice { get; set; }

        /// <summary>
        /// 产品形态（成品或配件）。
        /// </summary>
        [StringLength(30)]
        public string ProductForm { get; set; } = "成品";

        /// <summary>
        /// 详细描述（产品或物料的中文业务描述）。
        /// </summary>
        [StringLength(1000)]
        public string? DetailDescription { get; set; }

        /// <summary>
        /// 外文名称（对外贸易或英文资料名称）。
        /// </summary>
        [StringLength(200)]
        public string? ForeignName { get; set; }

        /// <summary>
        /// 外文描述（对外贸易或英文资料描述）。
        /// </summary>
        [StringLength(1000)]
        public string? ForeignDescription { get; set; }

        /// <summary>
        /// 供应商货号（供应商侧产品或物料编码）。
        /// </summary>
        [StringLength(80)]
        public string? SupplierItemCode { get; set; }

        /// <summary>
        /// 海关代码（进出口报关使用的商品编码）。
        /// </summary>
        [StringLength(80)]
        public string? CustomsCode { get; set; }

        /// <summary>
        /// 国际条形码（用于全球贸易或外部平台识别）。
        /// </summary>
        [StringLength(80)]
        public string? InternationalBarcode { get; set; }

        /// <summary>
        /// 材质（产品或物料主要材料说明）。
        /// </summary>
        [StringLength(120)]
        public string? MaterialTexture { get; set; }

        /// <summary>
        /// 特性（产品卖点、功能或物料特征）。
        /// </summary>
        [StringLength(200)]
        public string? Feature { get; set; }

        /// <summary>
        /// 自定义栏位1（预留给用户下拉或文本字段）。
        /// </summary>
        [StringLength(200)]
        public string? CustomField1 { get; set; }

        /// <summary>
        /// 自定义栏位2（预留给用户下拉或文本字段）。
        /// </summary>
        [StringLength(200)]
        public string? CustomField2 { get; set; }

        /// <summary>
        /// 自定义栏位3（预留给日期或文本字段）。
        /// </summary>
        public DateOnly? CustomDate3 { get; set; }

        /// <summary>
        /// 自定义栏位4（预留给用户下拉或文本字段）。
        /// </summary>
        [StringLength(200)]
        public string? CustomField4 { get; set; }

        /// <summary>
        /// 自定义栏位5（预留给用户下拉或文本字段）。
        /// </summary>
        [StringLength(200)]
        public string? CustomField5 { get; set; }

        /// <summary>
        /// 自定义栏位6（预留给用户下拉或文本字段）。
        /// </summary>
        [StringLength(200)]
        public string? CustomField6 { get; set; }

        /// <summary>
        /// 自定义栏位7（预留给用户下拉或文本字段）。
        /// </summary>
        [StringLength(200)]
        public string? CustomField7 { get; set; }

        /// <summary>
        /// 自定义栏位8（预留给用户下拉或文本字段）。
        /// </summary>
        [StringLength(200)]
        public string? CustomField8 { get; set; }

        /// <summary>
        /// 自定义栏位9（预留给用户下拉或文本字段）。
        /// </summary>
        [StringLength(200)]
        public string? CustomField9 { get; set; }

        /// <summary>
        /// 网址（产品官网、资料页或供应商链接）。
        /// </summary>
        [StringLength(300)]
        public string? Website { get; set; }

        /// <summary>
        /// 备注（产品资料补充说明）。
        /// </summary>
        [StringLength(1000)]
        public string? Remarks { get; set; }

        /// <summary>
        /// 存档路径（旧字段保留；附件统一使用通用文档管理）。
        /// </summary>
        [StringLength(300)]
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
        /// 其他包装数量（自定义包装包含的基本单位数量）。
        /// </summary>
        public decimal? OtherPackageQuantity { get; set; }

        /// <summary>
        /// 单个包装长宽高（厘米）。
        /// </summary>
        [StringLength(80)]
        public string? SinglePackageSize { get; set; } = "2 x 2 x 2";

        /// <summary>
        /// 小包包装长宽高（厘米）。
        /// </summary>
        [StringLength(80)]
        public string? InnerPackageSize { get; set; } = "10 x 10 x 10";

        /// <summary>
        /// 箱装包装长宽高（厘米）。
        /// </summary>
        [StringLength(80)]
        public string? CartonPackageSize { get; set; } = "20 x 20 x 20";

        /// <summary>
        /// 其他包装长宽高（厘米）。
        /// </summary>
        [StringLength(80)]
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
        /// 唛头描述（包装外箱唛头或运输标识）。
        /// </summary>
        [StringLength(1000)]
        public string? ShippingMark { get; set; }

        /// <summary>
        /// 启用序列号（需要按单件序列追踪）。
        /// </summary>
        public bool EnableSerialNumber { get; set; }

        /// <summary>
        /// 启用批次（需要按批次追踪）。
        /// </summary>
        public bool EnableBatch { get; set; } = true;

        /// <summary>
        /// 启用保质期（需要记录有效期天数）。
        /// </summary>
        public bool EnableShelfLife { get; set; } = true;

        /// <summary>
        /// 保质期天数（启用保质期时使用）。
        /// </summary>
        public int ShelfLifeDays { get; set; } = 180;

        /// <summary>
        /// 启用保养期（需要记录建议保养周期）。
        /// </summary>
        public bool EnableMaintenancePeriod { get; set; }

        /// <summary>
        /// 保养期天数（启用保养期时使用）。
        /// </summary>
        public int MaintenancePeriodDays { get; set; } = 365;

        /// <summary>
        /// BOM 明细集合（产品组成物料清单）。
        /// </summary>
        public ICollection<MaterialItemBomLine> BomLines { get; set; } = new List<MaterialItemBomLine>();

        /// <summary>
        /// 售价设置集合（产品不同价格类别的售价）。
        /// </summary>
        public ICollection<MaterialItemPriceLine> PriceLines { get; set; } = new List<MaterialItemPriceLine>();

    }
}
