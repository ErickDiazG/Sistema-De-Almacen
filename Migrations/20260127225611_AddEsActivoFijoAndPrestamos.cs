using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema_Almacen.Migrations
{
    /// <inheritdoc />
    public partial class AddEsActivoFijoAndPrestamos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Agregar columna EsActivoFijo a la tabla Productos
            migrationBuilder.AddColumn<bool>(
                name: "EsActivoFijo",
                table: "Productos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Crear tabla Prestamos
            migrationBuilder.CreateTable(
                name: "Prestamos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductoId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioSolicitante = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Departamento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaSalida = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaEsperadaRegreso = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaDevolucionReal = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    Estatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Comentarios = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UsuarioRegistroId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prestamos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prestamos_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Prestamos_Usuarios_UsuarioRegistroId",
                        column: x => x.UsuarioRegistroId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prestamos_ProductoId",
                table: "Prestamos",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Prestamos_UsuarioRegistroId",
                table: "Prestamos",
                column: "UsuarioRegistroId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Prestamos");

            migrationBuilder.DropColumn(
                name: "EsActivoFijo",
                table: "Productos");
        }
    }
}
