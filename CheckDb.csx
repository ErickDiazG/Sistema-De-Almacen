using System;
using System.IO;
using Microsoft.Data.Sqlite;

var dbPath = "SistemaAlmacen.db";
Console.WriteLine($"Verificando DB en: {Path.GetFullPath(dbPath)}");

if (!File.Exists(dbPath))
{
    Console.WriteLine("❌ ERROR: No se encuentra el archivo de base de datos.");
    return;
}

using (var conn = new SqliteConnection($"Data Source={dbPath}"))
{
    conn.Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = "PRAGMA table_info(Productos);";
    
    Console.WriteLine("Columnas en 'Productos':");
    using (var reader = cmd.ExecuteReader())
    {
        while (reader.Read())
        {
            Console.WriteLine($"- {reader["name"]} ({reader["type"]})");
        }
    }
}
