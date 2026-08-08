using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestionGastos.Api.Data.Migraciones
{
    /// <summary>
    /// La moneda deja de ser un enum en la columna y pasa a ser una tabla catalogo con
    /// foreign key, para poder sumar monedas sin tocar codigo.
    ///
    /// Lo que genera EF por defecto no sirve tal cual y esta reescrito a mano:
    ///  * EF tira la columna vieja y crea la nueva con default "", perdiendo la moneda de
    ///    cada movimiento y dejando valores que la foreign key rechaza. Aca los valores se
    ///    traducen ('Pesos' -> 'ARS', 'Dolares' -> 'USD') antes de borrar nada.
    ///  * El orden de indices esta invertido: MySQL no deja tirar el indice que empieza
    ///    por UsuarioId mientras sea el unico que sostiene esa foreign key, asi que primero
    ///    se crea el nuevo y despues se borra el viejo.
    /// </summary>
    public partial class ConvertirMonedaEnCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. El catalogo tiene que existir antes de que nadie lo referencie.
            migrationBuilder.CreateTable(
                name: "Monedas",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Simbolo = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Decimales = table.Column<int>(type: "int", nullable: false),
                    EsPredeterminada = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Monedas", x => x.Codigo);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Monedas",
                columns: new[] { "Codigo", "Decimales", "EsPredeterminada", "Nombre", "Orden", "Simbolo" },
                values: new object[,]
                {
                    { "ARS", 2, true, "Pesos", 1, "$" },
                    { "USD", 2, false, "Dolares", 2, "US$" }
                });

            // 2. La columna nueva nace con la moneda predeterminada, para que ninguna fila
            //    quede con un valor que la foreign key vaya a rechazar.
            migrationBuilder.AddColumn<string>(
                name: "MonedaCodigo",
                table: "Movimientos",
                type: "char(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                defaultValue: "ARS")
                .Annotation("MySql:CharSet", "utf8mb4");

            // 3. Se traducen los movimientos que estaban en dolares. Los que estaban en
            //    pesos ya quedaron en ARS por el default.
            migrationBuilder.Sql(
                "UPDATE `Movimientos` SET `MonedaCodigo` = 'USD' WHERE `Moneda` = 'Dolares';");

            // El default cumplio su funcion en el paso 2; el modelo no lo declara, asi que
            // se saca para que el esquema y el modelo no queden en desacuerdo.
            migrationBuilder.Sql(
                "ALTER TABLE `Movimientos` ALTER COLUMN `MonedaCodigo` DROP DEFAULT;");

            // 4. Recien ahora se puede soltar la columna vieja y su indice.
            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_UsuarioId_Fecha_MonedaCodigo",
                table: "Movimientos",
                columns: new[] { "UsuarioId", "Fecha", "MonedaCodigo" });

            migrationBuilder.DropIndex(
                name: "IX_Movimientos_UsuarioId_Fecha_Moneda",
                table: "Movimientos");

            migrationBuilder.DropColumn(
                name: "Moneda",
                table: "Movimientos");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_MonedaCodigo",
                table: "Movimientos",
                column: "MonedaCodigo");

            migrationBuilder.AddForeignKey(
                name: "FK_Movimientos_Monedas_MonedaCodigo",
                table: "Movimientos",
                column: "MonedaCodigo",
                principalTable: "Monedas",
                principalColumn: "Codigo",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Movimientos_Monedas_MonedaCodigo",
                table: "Movimientos");

            migrationBuilder.AddColumn<string>(
                name: "Moneda",
                table: "Movimientos",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Pesos")
                .Annotation("MySql:CharSet", "utf8mb4");

            // Vuelta atras de la traduccion, para no perder la moneda al revertir.
            migrationBuilder.Sql(
                "UPDATE `Movimientos` SET `Moneda` = 'Dolares' WHERE `MonedaCodigo` = 'USD';");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_UsuarioId_Fecha_Moneda",
                table: "Movimientos",
                columns: new[] { "UsuarioId", "Fecha", "Moneda" });

            migrationBuilder.DropIndex(
                name: "IX_Movimientos_UsuarioId_Fecha_MonedaCodigo",
                table: "Movimientos");

            migrationBuilder.DropIndex(
                name: "IX_Movimientos_MonedaCodigo",
                table: "Movimientos");

            migrationBuilder.DropColumn(
                name: "MonedaCodigo",
                table: "Movimientos");

            migrationBuilder.DropTable(
                name: "Monedas");
        }
    }
}
