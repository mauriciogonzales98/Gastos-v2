using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestionGastos.Api.Data.Migraciones
{
    /// <inheritdoc />
    public partial class CrearCategoriasYMovimientos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nombre = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    EsDelSistema = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaBajaUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categorias_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Movimientos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UsuarioId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CategoriaId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movimientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Movimientos_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimientos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Categorias",
                columns: new[] { "Id", "EsDelSistema", "FechaBajaUtc", "Nombre", "Tipo", "UsuarioId" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-4000-8000-000000000001"), true, null, "Comida", "Gasto", null },
                    { new Guid("a1000000-0000-4000-8000-000000000002"), true, null, "Transporte", "Gasto", null },
                    { new Guid("a1000000-0000-4000-8000-000000000003"), true, null, "Vivienda", "Gasto", null },
                    { new Guid("a1000000-0000-4000-8000-000000000004"), true, null, "Servicios", "Gasto", null },
                    { new Guid("a1000000-0000-4000-8000-000000000005"), true, null, "Salud", "Gasto", null },
                    { new Guid("a1000000-0000-4000-8000-000000000006"), true, null, "Ocio", "Gasto", null },
                    { new Guid("a1000000-0000-4000-8000-000000000007"), true, null, "Otros", "Gasto", null },
                    { new Guid("b2000000-0000-4000-8000-000000000001"), true, null, "Sueldo", "Ingreso", null },
                    { new Guid("b2000000-0000-4000-8000-000000000002"), true, null, "Ingreso extra", "Ingreso", null },
                    { new Guid("b2000000-0000-4000-8000-000000000003"), true, null, "Otros", "Ingreso", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_UsuarioId_Tipo",
                table: "Categorias",
                columns: new[] { "UsuarioId", "Tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_CategoriaId",
                table: "Movimientos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_UsuarioId_Fecha",
                table: "Movimientos",
                columns: new[] { "UsuarioId", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Movimientos");

            migrationBuilder.DropTable(
                name: "Categorias");
        }
    }
}
