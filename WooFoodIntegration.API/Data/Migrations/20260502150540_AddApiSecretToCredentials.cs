using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WooFoodIntegration.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApiSecretToCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiSecret",
                table: "TenantCredentials",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiSecret",
                table: "TenantCredentials");
        }
    }
}
