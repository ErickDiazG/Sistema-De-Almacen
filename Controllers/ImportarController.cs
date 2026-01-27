using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models;
using System.Security.Claims;

namespace Sistema_Almacen.Controllers
{
    [Authorize]
    public class ImportarController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ImportarController(ApplicationDbContext context)
        {
            _context = context;
            // Configurar licencia de EPPlus (NonCommercial para uso no comercial)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// GET: Muestra la interfaz de importación
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// GET: Genera y descarga la plantilla Excel con los encabezados correctos
        /// </summary>
        public IActionResult DescargarPlantilla()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Inventario");

            // Encabezados
            worksheet.Cells[1, 1].Value = "SKU";
            worksheet.Cells[1, 2].Value = "Nombre";
            worksheet.Cells[1, 3].Value = "Costo";
            worksheet.Cells[1, 4].Value = "Cantidad";
            worksheet.Cells[1, 5].Value = "Ubicacion";

            // Estilo de encabezados
            using (var range = worksheet.Cells[1, 1, 1, 5])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(79, 129, 189));
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // Ejemplo de datos (fila 2)
            worksheet.Cells[2, 1].Value = "PROD-001";
            worksheet.Cells[2, 2].Value = "Producto de Ejemplo";
            worksheet.Cells[2, 3].Value = 150.50;
            worksheet.Cells[2, 4].Value = 100;
            worksheet.Cells[2, 5].Value = "A1-01";

            // Ajustar ancho de columnas
            worksheet.Column(1).Width = 15;
            worksheet.Column(2).Width = 40;
            worksheet.Column(3).Width = 12;
            worksheet.Column(4).Width = 12;
            worksheet.Column(5).Width = 15;

            // Agregar instrucciones
            var instrucciones = package.Workbook.Worksheets.Add("Instrucciones");
            instrucciones.Cells[1, 1].Value = "INSTRUCCIONES DE LLENADO";
            instrucciones.Cells[1, 1].Style.Font.Bold = true;
            instrucciones.Cells[1, 1].Style.Font.Size = 14;

            instrucciones.Cells[3, 1].Value = "SKU: Código único del producto (obligatorio)";
            instrucciones.Cells[4, 1].Value = "Nombre: Nombre/descripción del producto (obligatorio)";
            instrucciones.Cells[5, 1].Value = "Costo: Costo unitario del producto (número >= 0)";
            instrucciones.Cells[6, 1].Value = "Cantidad: Cantidad inicial a ingresar (número entero >= 0)";
            instrucciones.Cells[7, 1].Value = "Ubicacion: Código de ubicación en almacén (debe existir en el sistema)";

            instrucciones.Cells[9, 1].Value = "NOTAS:";
            instrucciones.Cells[9, 1].Style.Font.Bold = true;
            instrucciones.Cells[10, 1].Value = "- Si el SKU ya existe, se agregará el lote al producto existente.";
            instrucciones.Cells[11, 1].Value = "- Si el SKU no existe, se creará un nuevo producto.";
            instrucciones.Cells[12, 1].Value = "- La ubicación debe existir previamente en el sistema.";
            instrucciones.Cells[13, 1].Value = "- Elimine la fila de ejemplo antes de importar.";

            instrucciones.Column(1).Width = 60;

            var content = package.GetAsByteArray();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PlantillaImportacion.xlsx");
        }

        /// <summary>
        /// POST: Procesa el archivo Excel y realiza la importación masiva
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CargarExcel(IFormFile archivoExcel)
        {
            var erroresImportacion = new List<string>();
            int filasExitosas = 0;
            int filasConError = 0;

            if (archivoExcel == null || archivoExcel.Length == 0)
            {
                ViewBag.TipoMensaje = "danger";
                ViewBag.Mensaje = "Por favor, seleccione un archivo Excel válido.";
                return View("Index");
            }

            // Validar extensión
            var extension = Path.GetExtension(archivoExcel.FileName).ToLowerInvariant();
            if (extension != ".xlsx")
            {
                ViewBag.TipoMensaje = "danger";
                ViewBag.Mensaje = "Solo se permiten archivos .xlsx (Excel 2007+).";
                return View("Index");
            }

            // Obtener ID del usuario actual
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int usuarioId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 1;

            // Obtener sucursal por defecto (la primera disponible)
            var sucursalPorDefecto = await _context.Sucursales.FirstOrDefaultAsync();
            if (sucursalPorDefecto == null)
            {
                ViewBag.TipoMensaje = "danger";
                ViewBag.Mensaje = "Error: No hay sucursales configuradas en el sistema.";
                return View("Index");
            }

            // Obtener categoría por defecto
            var categoriaPorDefecto = await _context.Categorias.FirstOrDefaultAsync();
            if (categoriaPorDefecto == null)
            {
                ViewBag.TipoMensaje = "danger";
                ViewBag.Mensaje = "Error: No hay categorías configuradas en el sistema.";
                return View("Index");
            }

            try
            {
                using var stream = new MemoryStream();
                await archivoExcel.CopyToAsync(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    ViewBag.TipoMensaje = "danger";
                    ViewBag.Mensaje = "El archivo Excel no contiene hojas de trabajo.";
                    return View("Index");
                }

                int rowCount = worksheet.Dimension?.Rows ?? 0;

                if (rowCount < 2)
                {
                    ViewBag.TipoMensaje = "warning";
                    ViewBag.Mensaje = "El archivo no contiene datos para importar (solo encabezados o vacío).";
                    return View("Index");
                }

                // Procesar fila por fila (empezando en la 2)
                for (int row = 2; row <= rowCount; row++)
                {
                    // Usar transacción por fila para atomicidad
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        // Leer valores de la fila
                        string? sku = worksheet.Cells[row, 1].Text?.Trim();
                        string? nombre = worksheet.Cells[row, 2].Text?.Trim();
                        string? costoStr = worksheet.Cells[row, 3].Text?.Trim();
                        string? cantidadStr = worksheet.Cells[row, 4].Text?.Trim();
                        string? ubicacionCodigo = worksheet.Cells[row, 5].Text?.Trim();

                        // Validaciones
                        if (string.IsNullOrWhiteSpace(sku))
                        {
                            erroresImportacion.Add($"Fila {row}: SKU vacío o inválido.");
                            filasConError++;
                            await transaction.RollbackAsync();
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(nombre))
                        {
                            erroresImportacion.Add($"Fila {row}: Nombre vacío o inválido.");
                            filasConError++;
                            await transaction.RollbackAsync();
                            continue;
                        }

                        if (!decimal.TryParse(costoStr, out decimal costo) || costo < 0)
                        {
                            erroresImportacion.Add($"Fila {row}: Costo inválido (debe ser un número >= 0).");
                            filasConError++;
                            await transaction.RollbackAsync();
                            continue;
                        }

                        if (!int.TryParse(cantidadStr, out int cantidad) || cantidad < 0)
                        {
                            erroresImportacion.Add($"Fila {row}: Cantidad inválida (debe ser un número entero >= 0).");
                            filasConError++;
                            await transaction.RollbackAsync();
                            continue;
                        }

                        // Buscar ubicación por código
                        var ubicacion = await _context.Ubicaciones.FirstOrDefaultAsync(u => u.Codigo == ubicacionCodigo);
                        if (ubicacion == null)
                        {
                            erroresImportacion.Add($"Fila {row}: Ubicación '{ubicacionCodigo}' no encontrada en el sistema.");
                            filasConError++;
                            await transaction.RollbackAsync();
                            continue;
                        }

                        // Buscar si el producto ya existe por SKU
                        var productoExistente = await _context.Productos.FirstOrDefaultAsync(p => p.SKU == sku);
                        int productoId;

                        if (productoExistente == null)
                        {
                            // ESCENARIO A: Producto Nuevo
                            var nuevoProducto = new Producto
                            {
                                SKU = sku,
                                Nombre = nombre,
                                StockMinimo = 5, // Valor por defecto
                                PrecioVenta = costo * 1.3m, // Precio de venta sugerido (+30%)
                                CostoPromedio = costo,
                                CategoriaId = categoriaPorDefecto.Id
                            };

                            _context.Productos.Add(nuevoProducto);
                            await _context.SaveChangesAsync();
                            productoId = nuevoProducto.Id;
                        }
                        else
                        {
                            // ESCENARIO B: Producto Existente
                            productoId = productoExistente.Id;
                        }

                        // CREACIÓN DE LOTE (FIFO)
                        var nuevoLote = new LoteInventario
                        {
                            ProductoId = productoId,
                            UbicacionId = ubicacion.Id,
                            SucursalId = sucursalPorDefecto.Id,
                            CantidadInicial = cantidad,
                            StockActual = cantidad,
                            CostoUnitario = costo,
                            FechaEntrada = DateTime.Now
                        };

                        _context.LotesInventario.Add(nuevoLote);

                        // AUDITORÍA: Registro de movimiento
                        var movimiento = new MovimientoAlmacen
                        {
                            Fecha = DateTime.Now,
                            UsuarioId = usuarioId,
                            Tipo = TipoMovimiento.Entrada,
                            Cantidad = cantidad,
                            Referencia = $"Importación Excel - SKU: {sku}"
                        };

                        _context.MovimientosAlmacen.Add(movimiento);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        filasExitosas++;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        erroresImportacion.Add($"Fila {row}: Error interno - {ex.Message}");
                        filasConError++;
                    }
                }

                // Preparar mensaje de resultado
                if (filasConError == 0)
                {
                    ViewBag.TipoMensaje = "success";
                    ViewBag.Mensaje = $"✅ Importación completada exitosamente. Se procesaron {filasExitosas} registros.";
                }
                else if (filasExitosas > 0)
                {
                    ViewBag.TipoMensaje = "warning";
                    ViewBag.Mensaje = $"⚠️ Importación parcial: {filasExitosas} exitosas, {filasConError} con errores.";
                }
                else
                {
                    ViewBag.TipoMensaje = "danger";
                    ViewBag.Mensaje = $"❌ Importación fallida: {filasConError} filas con errores.";
                }

                ViewBag.Errores = erroresImportacion;
            }
            catch (Exception ex)
            {
                ViewBag.TipoMensaje = "danger";
                ViewBag.Mensaje = $"Error al procesar el archivo: {ex.Message}";
            }

            return View("Index");
        }
    }
}
