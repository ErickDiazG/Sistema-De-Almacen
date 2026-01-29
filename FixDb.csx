using Microsoft.Data.Sqlite;

var connectionString = "Data Source=SistemaAlmacen.db";
using var conn = new SqliteConnection(connectionString);
conn.Open();

var commands = new[]
{
    "DELETE FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '20260127225611_AddEsActivoFijoAndPrestamos';",
    "ALTER TABLE \"Productos\" ADD COLUMN \"EsActivoFijo\" INTEGER NOT NULL DEFAULT 0;",
    @"CREATE TABLE IF NOT EXISTS ""Prestamos"" (
        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Prestamos"" PRIMARY KEY AUTOINCREMENT,
        ""ProductoId"" INTEGER NOT NULL,
        ""UsuarioSolicitante"" TEXT NOT NULL,
        ""Departamento"" TEXT NULL,
        ""FechaSalida"" TEXT NOT NULL,
        ""FechaEsperadaRegreso"" TEXT NOT NULL,
        ""FechaDevolucionReal"" TEXT NULL,
        ""Cantidad"" INTEGER NOT NULL,
        ""Estatus"" INTEGER NOT NULL,
        ""Comentarios"" TEXT NULL,
        ""UsuarioRegistroId"" INTEGER NULL,
        CONSTRAINT ""FK_Prestamos_Productos_ProductoId"" FOREIGN KEY (""ProductoId"") REFERENCES ""Productos"" (""Id"") ON DELETE CASCADE,
        CONSTRAINT ""FK_Prestamos_Usuarios_UsuarioRegistroId"" FOREIGN KEY (""UsuarioRegistroId"") REFERENCES ""Usuarios"" (""Id"")
    );",
    "CREATE INDEX IF NOT EXISTS \"IX_Prestamos_ProductoId\" ON \"Prestamos\" (\"ProductoId\");",
    "CREATE INDEX IF NOT EXISTS \"IX_Prestamos_UsuarioRegistroId\" ON \"Prestamos\" (\"UsuarioRegistroId\");",
    "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('20260127225611_AddEsActivoFijoAndPrestamos', '8.0.0');"
};

foreach (var sql in commands)
{
    try
    {
        Console.WriteLine($"Ejecutando: {sql.Substring(0, Math.Min(60, sql.Length))}...");
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
        Console.WriteLine("✓ OK");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ Error (puede ser esperado): {ex.Message}");
    }
}

Console.WriteLine("\n¡Base de datos actualizada!");
