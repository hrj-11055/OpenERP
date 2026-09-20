using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenERP.Logistics.Migrations
{
    /// <summary>
    /// 修复旧版车间仓库表缺少创建人和修改人审计字段的问题。
    /// </summary>
    public partial class RepairWorkshopWarehouseAuditColumns : Migration
    {
        /// <summary>
        /// 为车间仓库主表和库位从表补齐审计人员字段。
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.LG_WorkshopWarehouse', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH(N'dbo.LG_WorkshopWarehouse', N'CreatedBy') IS NULL
                        ALTER TABLE dbo.LG_WorkshopWarehouse ADD CreatedBy NVARCHAR(50) NULL;

                    IF COL_LENGTH(N'dbo.LG_WorkshopWarehouse', N'UpdatedBy') IS NULL
                        ALTER TABLE dbo.LG_WorkshopWarehouse ADD UpdatedBy NVARCHAR(50) NULL;
                END

                IF OBJECT_ID(N'dbo.LG_WarehouseLocation', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH(N'dbo.LG_WarehouseLocation', N'CreatedBy') IS NULL
                        ALTER TABLE dbo.LG_WarehouseLocation ADD CreatedBy NVARCHAR(50) NULL;

                    IF COL_LENGTH(N'dbo.LG_WarehouseLocation', N'UpdatedBy') IS NULL
                        ALTER TABLE dbo.LG_WarehouseLocation ADD UpdatedBy NVARCHAR(50) NULL;
                END
                """);
        }

        /// <summary>
        /// 回退本次审计人员字段修复。
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.LG_WarehouseLocation', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH(N'dbo.LG_WarehouseLocation', N'UpdatedBy') IS NOT NULL
                        ALTER TABLE dbo.LG_WarehouseLocation DROP COLUMN UpdatedBy;

                    IF COL_LENGTH(N'dbo.LG_WarehouseLocation', N'CreatedBy') IS NOT NULL
                        ALTER TABLE dbo.LG_WarehouseLocation DROP COLUMN CreatedBy;
                END

                IF OBJECT_ID(N'dbo.LG_WorkshopWarehouse', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH(N'dbo.LG_WorkshopWarehouse', N'UpdatedBy') IS NOT NULL
                        ALTER TABLE dbo.LG_WorkshopWarehouse DROP COLUMN UpdatedBy;

                    IF COL_LENGTH(N'dbo.LG_WorkshopWarehouse', N'CreatedBy') IS NOT NULL
                        ALTER TABLE dbo.LG_WorkshopWarehouse DROP COLUMN CreatedBy;
                END
                """);
        }
    }
}
