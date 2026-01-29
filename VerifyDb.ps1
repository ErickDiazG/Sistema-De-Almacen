$dbPath = "$PSScriptRoot\SistemaAlmacen.db"
Add-Type -Path "$PSScriptRoot\bin\Debug\net8.0\Microsoft.Data.Sqlite.dll"

$conn = New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$dbPath")
$conn.Open()

Write-Host "Verificando columnas en Productos..."
$cmd = $conn.CreateCommand()
$cmd.CommandText = "PRAGMA table_info(Productos);"
$reader = $cmd.ExecuteReader()

while ($reader.Read()) {
    $name = $reader["name"]
    $type = $reader["type"]
    Write-Host "- $name ($type)"
}

$conn.Close()
