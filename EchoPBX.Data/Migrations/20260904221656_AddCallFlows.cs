using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoPBX.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCallFlows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "call_flows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    InternalNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    DefinitionJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_call_flows", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_call_flows_Slug",
                table: "call_flows",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "call_flows");
        }
    }
}
