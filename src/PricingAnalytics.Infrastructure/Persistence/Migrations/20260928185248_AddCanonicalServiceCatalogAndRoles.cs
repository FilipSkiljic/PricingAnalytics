using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PricingAnalytics.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCanonicalServiceCatalogAndRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing accounts become ordinary users. An empty string would fail to parse
            // back into the BusinessRole enum on the next read.
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Businesses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "User");

            migrationBuilder.CreateTable(
                name: "CanonicalServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequiredPhrase = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ExcludedWords = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonicalServices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalServices_Slug",
                table: "CanonicalServices",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CanonicalServices");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Businesses");
        }
    }
}
