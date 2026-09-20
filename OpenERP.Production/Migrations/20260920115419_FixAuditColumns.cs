/*
 * File: OpenERP.Production/Migrations/20260920115419_FixAuditColumns.cs
 * Description: 为生产模块三张表补齐 BaseEntity 审计列（CreatedBy/UpdatedBy）。
 *              注:本迁移手工裁剪过 —— 生成器基于旧快照(无前缀表名)产出的 RenameTable/DropPK
 *              与实际数据库(PRD_ 前缀表已存在)不符,已移除;仅保留数据库真正缺失的审计列。
 */
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenERP.Production.Migrations
{
    /// <inheritdoc />
    public partial class FixAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PRD_WorkCenter",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "PRD_WorkCenter",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PRD_ProductionOrder",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "PRD_ProductionOrder",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PRD_ProductionOrderItem",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "PRD_ProductionOrderItem",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PRD_WorkCenter");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "PRD_WorkCenter");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PRD_ProductionOrder");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "PRD_ProductionOrder");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PRD_ProductionOrderItem");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "PRD_ProductionOrderItem");
        }
    }
}
