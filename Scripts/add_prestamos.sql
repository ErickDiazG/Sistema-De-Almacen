-- Script para agregar la tabla Prestamos y columna EsActivoFijo
-- Ejecutar en SQLite

-- Agregar columna EsActivoFijo a Productos (si no existe)
ALTER TABLE Productos ADD COLUMN EsActivoFijo INTEGER NOT NULL DEFAULT 0;

-- Crear tabla de Prestamos
CREATE TABLE IF NOT EXISTS Prestamos (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductoId INTEGER NOT NULL,
    UsuarioSolicitante TEXT NOT NULL,
    Departamento TEXT NULL,
    FechaSalida TEXT NOT NULL,
    FechaEsperadaRegreso TEXT NOT NULL,
    FechaDevolucionReal TEXT NULL,
    Estatus INTEGER NOT NULL DEFAULT 0,
    Cantidad INTEGER NOT NULL DEFAULT 1,
    Comentarios TEXT NULL,
    UsuarioRegistroId INTEGER NULL,
    FOREIGN KEY (ProductoId) REFERENCES Productos(Id) ON DELETE CASCADE,
    FOREIGN KEY (UsuarioRegistroId) REFERENCES Usuarios(Id)
);

-- Crear índices
CREATE INDEX IF NOT EXISTS IX_Prestamos_ProductoId ON Prestamos(ProductoId);
CREATE INDEX IF NOT EXISTS IX_Prestamos_UsuarioRegistroId ON Prestamos(UsuarioRegistroId);
