using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenERP.CRM.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerSupplierRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCustomerRole",
                table: "CRM_Customer",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSupplierRole",
                table: "CRM_Customer",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCustomerRole",
                table: "CRM_Customer");

            migrationBuilder.DropColumn(
                name: "IsSupplierRole",
                table: "CRM_Customer");
        }
    }
}
