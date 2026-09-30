using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatterHarbor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrganizationRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                schema: "matterharbor",
                table: "organization_users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Administrator");

            migrationBuilder.Sql("ALTER TABLE matterharbor.organization_users ALTER COLUMN \"Role\" DROP DEFAULT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                schema: "matterharbor",
                table: "organization_users");
        }
    }
}
