using Microsoft.Data.Sqlite;
using System;

var connectionString = "Data Source=SistemaAlmacen.db";

try
{
    using (var connection = new SqliteConnection(connectionString))
    {
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Prestamos';";
        var name = command.ExecuteScalar() as string;

        if (!string.IsNullOrEmpty(name))
        {
            Console.WriteLine("SUCCESS: Table 'Prestamos' FOUND.");
        }
        else
        {
            Console.WriteLine("FAILURE: Table 'Prestamos' NOT FOUND.");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
