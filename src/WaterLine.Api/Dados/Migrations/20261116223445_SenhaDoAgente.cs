using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaterLine.Api.Dados.Migrations
{
    /// <inheritdoc />
    public partial class SenhaDoAgente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SenhaHash",
                table: "Agentes",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SenhaHash",
                table: "Agentes");
        }
    }
}
