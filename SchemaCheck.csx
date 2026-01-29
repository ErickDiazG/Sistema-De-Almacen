using Microsoft.Data.Sqlite;
using System;

var connectionString = "Data Source=SistemaAlmacen.db";

try
{
    using (var connection = new SqliteConnection(connectionString))
    {
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(Productos);";

        using (var reader = command.ExecuteReader())
        {
            Console.WriteLine("Columns in Productos table:");
            var found = false;
            while (reader.Read())
            {
                var name = reader.GetString(1);
                Console.WriteLine($"- {name}");
                if (name == "EsActivoFijo") found = true;
            }
            
            if (found) Console.WriteLine("\nSUCCESS: Column 'EsActivoFijo' FOUND.");
            else Console.WriteLine("\nFAILURE: Column 'EsActivoFijo' NOT FOUND.");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
