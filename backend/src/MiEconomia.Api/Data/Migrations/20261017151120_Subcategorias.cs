using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiEconomia.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Subcategorias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SubcategoriaId",
                table: "Movimientos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Subcategorias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreadoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subcategorias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subcategorias_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_SubcategoriaId",
                table: "Movimientos",
                column: "SubcategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Subcategorias_CategoriaId_Nombre",
                table: "Subcategorias",
                columns: new[] { "CategoriaId", "Nombre" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Movimientos_Subcategorias_SubcategoriaId",
                table: "Movimientos",
                column: "SubcategoriaId",
                principalTable: "Subcategorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Movimientos_Subcategorias_SubcategoriaId",
                table: "Movimientos");

            migrationBuilder.DropTable(
                name: "Subcategorias");

            migrationBuilder.DropIndex(
                name: "IX_Movimientos_SubcategoriaId",
                table: "Movimientos");

            migrationBuilder.DropColumn(
                name: "SubcategoriaId",
                table: "Movimientos");
        }
    }
}
