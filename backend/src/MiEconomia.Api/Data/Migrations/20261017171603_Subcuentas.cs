using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiEconomia.Api.Data.Migrations
{
    /// <summary>
    /// Reparte el dinero de cada cuenta en subcuentas con moneda. Los datos existentes no se pierden:
    /// cada cuenta recibe una subcuenta "Disponible" en UYU con su saldo inicial, y sus movimientos
    /// y transferencias pasan a esa subcuenta.
    /// </summary>
    public partial class Subcuentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Subcuentas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CuentaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Moneda = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    SaldoInicial = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreadoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subcuentas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subcuentas_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subcuentas_CuentaId_Nombre",
                table: "Subcuentas",
                columns: new[] { "CuentaId", "Nombre" },
                unique: true);

            // Una subcuenta "Disponible" por cuenta, con el saldo inicial que tenía la cuenta.
            migrationBuilder.Sql("""
                INSERT INTO "Subcuentas" ("Id", "CuentaId", "Nombre", "Moneda", "SaldoInicial", "CreadoEn")
                SELECT gen_random_uuid(), "Id", 'Disponible', 'UYU', "SaldoInicial", "CreadoEn"
                FROM "Cuentas";
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "SubcuentaId",
                table: "Movimientos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SubcuentaOrigenId",
                table: "Transferencias",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SubcuentaDestinoId",
                table: "Transferencias",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "MontoDestino",
                table: "Transferencias",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            // Cada movimiento y transferencia pasa a la única subcuenta de su cuenta.
            migrationBuilder.Sql("""
                UPDATE "Movimientos" m SET "SubcuentaId" = s."Id"
                FROM "Subcuentas" s WHERE s."CuentaId" = m."CuentaId";

                UPDATE "Transferencias" t SET "SubcuentaOrigenId" = s."Id"
                FROM "Subcuentas" s WHERE s."CuentaId" = t."CuentaOrigenId";

                UPDATE "Transferencias" t SET "SubcuentaDestinoId" = s."Id"
                FROM "Subcuentas" s WHERE s."CuentaId" = t."CuentaDestinoId";
                """);

            migrationBuilder.DropColumn(
                name: "SaldoInicial",
                table: "Cuentas");

            // Ahora se puede transferir dentro de la misma cuenta, entre subcuentas distintas.
            migrationBuilder.DropCheckConstraint(
                name: "CK_Transferencias_Cuentas_Distintas",
                table: "Transferencias");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transferencias_Subcuentas_Distintas",
                table: "Transferencias",
                sql: "\"SubcuentaOrigenId\" <> \"SubcuentaDestinoId\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transferencias_MontoDestino_Positivo",
                table: "Transferencias",
                sql: "\"MontoDestino\" IS NULL OR \"MontoDestino\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_SubcuentaId",
                table: "Movimientos",
                column: "SubcuentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_SubcuentaOrigenId",
                table: "Transferencias",
                column: "SubcuentaOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_SubcuentaDestinoId",
                table: "Transferencias",
                column: "SubcuentaDestinoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Movimientos_Subcuentas_SubcuentaId",
                table: "Movimientos",
                column: "SubcuentaId",
                principalTable: "Subcuentas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transferencias_Subcuentas_SubcuentaOrigenId",
                table: "Transferencias",
                column: "SubcuentaOrigenId",
                principalTable: "Subcuentas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transferencias_Subcuentas_SubcuentaDestinoId",
                table: "Transferencias",
                column: "SubcuentaDestinoId",
                principalTable: "Subcuentas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <summary>
        /// Vuelve a una cuenta sin subcuentas: el saldo inicial de la cuenta es la suma de los de sus subcuentas
        /// (sin importar la moneda). Las transferencias entre subcuentas de una misma cuenta se pierden.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SaldoInicial",
                table: "Cuentas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE "Cuentas" c SET "SaldoInicial" = s."Total"
                FROM (SELECT "CuentaId", SUM("SaldoInicial") AS "Total" FROM "Subcuentas" GROUP BY "CuentaId") s
                WHERE s."CuentaId" = c."Id";

                DELETE FROM "Transferencias" WHERE "CuentaOrigenId" = "CuentaDestinoId";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Movimientos_Subcuentas_SubcuentaId",
                table: "Movimientos");

            migrationBuilder.DropForeignKey(
                name: "FK_Transferencias_Subcuentas_SubcuentaDestinoId",
                table: "Transferencias");

            migrationBuilder.DropForeignKey(
                name: "FK_Transferencias_Subcuentas_SubcuentaOrigenId",
                table: "Transferencias");

            migrationBuilder.DropTable(
                name: "Subcuentas");

            migrationBuilder.DropIndex(
                name: "IX_Transferencias_SubcuentaDestinoId",
                table: "Transferencias");

            migrationBuilder.DropIndex(
                name: "IX_Transferencias_SubcuentaOrigenId",
                table: "Transferencias");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transferencias_MontoDestino_Positivo",
                table: "Transferencias");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transferencias_Subcuentas_Distintas",
                table: "Transferencias");

            migrationBuilder.DropIndex(
                name: "IX_Movimientos_SubcuentaId",
                table: "Movimientos");

            migrationBuilder.DropColumn(
                name: "MontoDestino",
                table: "Transferencias");

            migrationBuilder.DropColumn(
                name: "SubcuentaDestinoId",
                table: "Transferencias");

            migrationBuilder.DropColumn(
                name: "SubcuentaOrigenId",
                table: "Transferencias");

            migrationBuilder.DropColumn(
                name: "SubcuentaId",
                table: "Movimientos");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transferencias_Cuentas_Distintas",
                table: "Transferencias",
                sql: "\"CuentaOrigenId\" <> \"CuentaDestinoId\"");
        }
    }
}
