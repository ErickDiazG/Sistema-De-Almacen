using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Sistema_Almacen.Data;
using Sistema_Almacen.Models;
using Sistema_Almacen.Models.ViewModels;
using System.Security.Claims;

namespace Sistema_Almacen.Controllers
{
    public class ImportarController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ImportarController(ApplicationDbContext context)
        {
            _context = context;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Previsualizar(IFormFile archivoExcel)
        {
            if (archivoExcel == null || archivoExcel.Length == 0 || !Path.GetExtension(archivoExcel.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error = "Por favor, sube un archivo .xlsx válido.";
                return View("Index");
            }

            var previewList = new List<ImportPreviewViewModel>();
            
            try 
            {
                using var stream = new MemoryStream();
                await archivoExcel.CopyToAsync(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                
                if (worksheet == null || (worksheet.Dimension?.Rows ?? 0) < 2)
                {
                    ViewBag.Error = "El archivo está vacío o no tiene formato correcto.";
                    return View("Index");
                }

                int rowCount = worksheet.Dimension.Rows;
                var ubicaciones = await _context.Ubicaciones.ToDictionaryAsync(u => u.Codigo, u => u.Id);
                var productosExistentes = await _context.Productos.Select(p => p.SKU).ToListAsync();

                for (int row = 2; row <= rowCount; row++)
                {
                    var item = new ImportPreviewViewModel
                    {
                        RowIndex = row,
                        SKU = worksheet.Cells[row, 1].Text?.Trim() ?? "",
                        Nombre = worksheet.Cells[row, 2].Text?.Trim() ?? "",
                        UbicacionCodigo = worksheet.Cells[row, 5].Text?.Trim() ?? ""
                    };

                    // Probar parseos numéricos
                    decimal.TryParse(worksheet.Cells[row, 3].Text?.Trim(), out decimal costo);
                    int.TryParse(worksheet.Cells[row, 4].Text?.Trim(), out int cantidad);
                    
                    item.Costo = costo;
                    item.Cantidad = cantidad;

                    // Validaciones
                    if (string.IsNullOrEmpty(item.SKU)) 
                    {
                        item.IsValid = false;
                        item.ErrorMessage = "SKU vacío";
                    }
                    else if (productosExistentes.Contains(item.SKU))
                    {
                        item.ExistsInDb = true;
                        item.ErrorMessage = "SKU ya existe (Se sumará stock)";
                    }

                    if (!ubicaciones.ContainsKey(item.UbicacionCodigo))
                    {
                        item.IsValid = false;
                        item.ErrorMessage += (string.IsNullOrEmpty(item.ErrorMessage) ? "" : " | ") + "Ubicación desconocida";
                    }

                    previewList.Add(item);
                }

                // Guardar temporalmente en TempData o enviarlo a la vista
                // Para simplificar y evitar problemas de tamaño en TempData, enviamos directo a la vista
                return View("Visualize", previewList);
            }
            catch(Exception ex)
            {
                ViewBag.Error = "Error al leer el archivo: " + ex.Message;
                return View("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Confirmar([FromBody] List<ImportPreviewViewModel> datosConfirmados)
        {
            if (datosConfirmados == null || !datosConfirmados.Any())
            {
                return Json(new { success = false, message = "No hay datos para procesar." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sucursal = await _context.Sucursales.FirstOrDefaultAsync();
                var categoria = await _context.Categorias.FirstOrDefaultAsync();
                var ubicaciones = await _context.Ubicaciones.ToDictionaryAsync(u => u.Codigo, u => u.Id);
                
                int procesados = 0;

                foreach (var item in datosConfirmados)
                {
                    if (!item.IsValid) continue; // Saltar inválidos si llegan aquí

                    var producto = await _context.Productos.FirstOrDefaultAsync(p => p.SKU == item.SKU);
                    int productoId = 0;

                    if (producto == null)
                    {
                        // Crear nuevo
                        var nuevo = new Producto
                        {
                            SKU = item.SKU,
                            Nombre = item.Nombre,
                            CostoPromedio = item.Costo,
                            PrecioVenta = item.Costo * 1.3m,
                            StockMinimo = 5,
                            CategoriaId = categoria?.Id ?? 1
                        };
                        _context.Productos.Add(nuevo);
                        await _context.SaveChangesAsync();
                        productoId = nuevo.Id;
                    }
                    else
                    {
                        productoId = producto.Id;
                    }

                    // Crear Lote
                    if (ubicaciones.TryGetValue(item.UbicacionCodigo, out int ubicacionId))
                    {
                        var lote = new LoteInventario
                        {
                            ProductoId = productoId,
                            CantidadInicial = item.Cantidad,
                            StockActual = item.Cantidad,
                            CostoUnitario = item.Costo,
                            FechaEntrada = DateTime.Now,
                            UbicacionId = ubicacionId,
                            SucursalId = sucursal?.Id ?? 1
                        };
                        _context.LotesInventario.Add(lote);
                        
                        // Movimiento
                        _context.MovimientosAlmacen.Add(new MovimientoAlmacen
                        {
                            Fecha = DateTime.Now,
                            UsuarioId = 1, // Default Admin
                            Tipo = TipoMovimiento.Entrada,
                            Cantidad = item.Cantidad,
                            Referencia = $"Importación Wizard - {item.SKU}"
                        });

                        procesados++;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = $"Se importaron {procesados} productos correctamente." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = ex.Message });
            }
        }
        
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

            worksheet.Column(1).Width = 15;
            worksheet.Column(2).Width = 40;
            
            var content = package.GetAsByteArray();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PlantillaImportacion.xlsx");
        }
    }
}
