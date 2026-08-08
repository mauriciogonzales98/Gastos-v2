using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionGastos.Api.Data.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarMonedaAMovimientos : Migration
    {
        /// <inheritdoc />
        // El orden de las operaciones esta cambiado respecto de lo que genera EF: la
        // foreign key de UsuarioId se apoya en el indice que empieza por esa columna, y
        // MySQL no deja tirarlo mientras sea el unico que le sirve. Por eso primero se
        // crea el indice nuevo (que tambien empieza por UsuarioId) y recien despues se
        // borra el viejo. En Down va al reves, por el mismo motivo.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue "Pesos" y no "" (lo que genera EF por defecto): los movimientos
            // cargados antes de que existiera la moneda quedan en pesos. Un "" no es un
            // valor valido del enum y romperia al leerlos.
            migrationBuilder.AddColumn<string>(
                name: "Moneda",
                table: "Movimientos",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Pesos")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_UsuarioId_Fecha_Moneda",
                table: "Movimientos",
                columns: new[] { "UsuarioId", "Fecha", "Moneda" });

            migrationBuilder.DropIndex(
                name: "IX_Movimientos_UsuarioId_Fecha",
                table: "Movimientos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_UsuarioId_Fecha",
                table: "Movimientos",
                columns: new[] { "UsuarioId", "Fecha" });

            migrationBuilder.DropIndex(
                name: "IX_Movimientos_UsuarioId_Fecha_Moneda",
                table: "Movimientos");

            migrationBuilder.DropColumn(
                name: "Moneda",
                table: "Movimientos");
        }
    }
}
