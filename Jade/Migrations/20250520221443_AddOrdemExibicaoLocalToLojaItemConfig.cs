using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jade.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdemExibicaoLocalToLojaItemConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrdemExibicaoLocal",
                table: "LojaItemConfiguracoes",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrdemExibicaoLocal",
                table: "LojaItemConfiguracoes");
        }
    }
}
