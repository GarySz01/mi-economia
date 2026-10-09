using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiEconomia.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Cuentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Primero nullable: los movimientos existentes se asignan a una cuenta más abajo.
            migrationBuilder.AddColumn<Guid>(
                name: "CuentaId",
                table: "Movimientos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cuentas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SaldoInicial = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    CreadoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuentas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cuentas_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transferencias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    CuentaOrigenId = table.Column<Guid>(type: "uuid", nullable: false),
                    CuentaDestinoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Nota = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CreadoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transferencias", x => x.Id);
                    table.CheckConstraint("CK_Transferencias_Cuentas_Distintas", "\"CuentaOrigenId\" <> \"CuentaDestinoId\"");
                    table.CheckConstraint("CK_Transferencias_Monto_Positivo", "\"Monto\" > 0");
                    table.ForeignKey(
                        name: "FK_Transferencias_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transferencias_Cuentas_CuentaDestinoId",
                        column: x => x.CuentaDestinoId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transferencias_Cuentas_CuentaOrigenId",
                        column: x => x.CuentaOrigenId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_CuentaId",
                table: "Movimientos",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_UsuarioId_Nombre",
                table: "Cuentas",
                columns: new[] { "UsuarioId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_CuentaDestinoId",
                table: "Transferencias",
                column: "CuentaDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_CuentaOrigenId",
                table: "Transferencias",
                column: "CuentaOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_UsuarioId_Fecha",
                table: "Transferencias",
                columns: new[] { "UsuarioId", "Fecha" });

            // Los usuarios que ya existían reciben la misma cuenta que se crea al registrarse
            // (CuentaPorDefecto), y todos sus movimientos pasan a esa cuenta.
            migrationBuilder.Sql("""
                INSERT INTO "Cuentas" ("Id", "UsuarioId", "Nombre", "Tipo", "SaldoInicial", "Color", "CreadoEn")
                SELECT gen_random_uuid(), u."Id", 'Efectivo', 'Efectivo', 0, '#16A34A', now()
                FROM "AspNetUsers" u;

                UPDATE "Movimientos" m
                SET "CuentaId" = c."Id"
                FROM "Cuentas" c
                WHERE c."UsuarioId" = m."UsuarioId";
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CuentaId",
                table: "Movimientos",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Movimientos_Cuentas_CuentaId",
                table: "Movimientos",
                column: "CuentaId",
                principalTable: "Cuentas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Movimientos_Cuentas_CuentaId",
                table: "Movimientos");

            migrationBuilder.DropTable(
                name: "Transferencias");

            migrationBuilder.DropTable(
                name: "Cuentas");

            migrationBuilder.DropIndex(
                name: "IX_Movimientos_CuentaId",
                table: "Movimientos");

            migrationBuilder.DropColumn(
                name: "CuentaId",
                table: "Movimientos");
        }
    }
}
