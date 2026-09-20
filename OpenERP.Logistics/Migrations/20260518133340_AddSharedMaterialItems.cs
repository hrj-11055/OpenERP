using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenERP.Logistics.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedMaterialItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BD_ItemMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemCategory = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Specification = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Origin = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BaseUnit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PurchaseUnit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    InventoryUnit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SalesUnit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PurchaseUnitRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InventoryUnitRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SalesUnitRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InventoryQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InventoryCapacity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PrimaryLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CurrentCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SuggestedPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ProductForm = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DetailDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ForeignName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ForeignDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SupplierItemCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CustomsCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    InternationalBarcode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    MaterialTexture = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Feature = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomField1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomField2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomDate3 = table.Column<DateOnly>(type: "date", nullable: true),
                    CustomField4 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomField5 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomField6 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomField7 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomField8 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomField9 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ArchivePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SinglePackageQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InnerPackageQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CartonPackageQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OtherPackageQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    SinglePackageSize = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    InnerPackageSize = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CartonPackageSize = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    OtherPackageSize = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SingleCbm = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InnerCbm = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CartonCbm = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OtherCbm = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    SingleNetWeight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InnerNetWeight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CartonNetWeight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OtherNetWeight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    SingleGrossWeight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InnerGrossWeight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CartonGrossWeight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OtherGrossWeight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ShippingMark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EnableSerialNumber = table.Column<bool>(type: "bit", nullable: false),
                    EnableBatch = table.Column<bool>(type: "bit", nullable: false),
                    EnableShelfLife = table.Column<bool>(type: "bit", nullable: false),
                    ShelfLifeDays = table.Column<int>(type: "int", nullable: false),
                    EnableMaintenancePeriod = table.Column<bool>(type: "bit", nullable: false),
                    MaintenancePeriodDays = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BD_ItemMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BD_ItemBomLine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialItemId = table.Column<int>(type: "int", nullable: false),
                    ComponentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ComponentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ComponentDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LossRatePercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityWithLoss = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CostPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CostAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BD_ItemBomLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BD_ItemBomLine_BD_ItemMaster_MaterialItemId",
                        column: x => x.MaterialItemId,
                        principalTable: "BD_ItemMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BD_ItemPriceLine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialItemId = table.Column<int>(type: "int", nullable: false),
                    PriceCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BD_ItemPriceLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BD_ItemPriceLine_BD_ItemMaster_MaterialItemId",
                        column: x => x.MaterialItemId,
                        principalTable: "BD_ItemMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BD_ItemBomLine_MaterialItemId_SortOrder",
                table: "BD_ItemBomLine",
                columns: new[] { "MaterialItemId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_BD_ItemMaster_ItemCategory_ItemCode",
                table: "BD_ItemMaster",
                columns: new[] { "ItemCategory", "ItemCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BD_ItemPriceLine_MaterialItemId_SortOrder",
                table: "BD_ItemPriceLine",
                columns: new[] { "MaterialItemId", "SortOrder" });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BD_ItemBomLine");

            migrationBuilder.DropTable(
                name: "BD_ItemPriceLine");

            migrationBuilder.DropTable(
                name: "BD_ItemMaster");
        }
    }
}
