-- Eliminar la migración vacía del historial para poder reaplicarla
DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260127225611_AddEsActivoFijoAndPrestamos';

-- Agregar la columna EsActivoFijo a la tabla Productos
ALTER TABLE "Productos" ADD COLUMN "EsActivoFijo" INTEGER NOT NULL DEFAULT 0;

-- Crear la tabla Prestamos
CREATE TABLE IF NOT EXISTS "Prestamos" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Prestamos" PRIMARY KEY AUTOINCREMENT,
    "ProductoId" INTEGER NOT NULL,
    "UsuarioSolicitante" TEXT NOT NULL,
    "Departamento" TEXT NULL,
    "FechaSalida" TEXT NOT NULL,
    "FechaEsperadaRegreso" TEXT NOT NULL,
    "FechaDevolucionReal" TEXT NULL,
    "Cantidad" INTEGER NOT NULL,
    "Estatus" INTEGER NOT NULL,
    "Comentarios" TEXT NULL,
    "UsuarioRegistroId" INTEGER NULL,
    CONSTRAINT "FK_Prestamos_Productos_ProductoId" FOREIGN KEY ("ProductoId") REFERENCES "Productos" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Prestamos_Usuarios_UsuarioRegistroId" FOREIGN KEY ("UsuarioRegistroId") REFERENCES "Usuarios" ("Id")
);

-- Crear índices
CREATE INDEX IF NOT EXISTS "IX_Prestamos_ProductoId" ON "Prestamos" ("ProductoId");
CREATE INDEX IF NOT EXISTS "IX_Prestamos_UsuarioRegistroId" ON "Prestamos" ("UsuarioRegistroId");

-- Volver a insertar el registro de migración
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20260127225611_AddEsActivoFijoAndPrestamos', '8.0.0');
