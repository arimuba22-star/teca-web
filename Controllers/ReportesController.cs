using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SWebEnergia.Models;
using System; // Agregado por si falta para DateTime
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using SWebEnergia.Models.ViewModels;


namespace SWebEnergia.Controllers
{
    public class ReportesController : Controller
    {
        private readonly EnergiaContext _context;

        // Constructor para inyectar el contexto de la base de datos
        public ReportesController(EnergiaContext context)
        {
            _context = context;
        }

        // GET: Reportes/Index
        public async Task<IActionResult> Index(string cliente, DateTime? desde, DateTime? hasta)
        {
            // Ventas (Utiliza el método auxiliar para Ventas)
            var reportesVentas = await ObtenerReportesFiltrados(cliente, desde, hasta);

            // Mantenimientos (Utiliza el nuevo método auxiliar para Mantenimientos)
            var reportesMantenimientos = await ObtenerReportesMantenimientosFiltrados(cliente, desde, hasta);

            var vm = new ReporteGeneralViewModel
            {
                ReportesVentas = reportesVentas,
                ReportesMantenimientos = reportesMantenimientos
            };

            return View(vm);
        }


        // **********************************************
        // MÉTODOS AUXILIARES DE FILTRADO
        // **********************************************

        private async Task<List<Reporte>> ObtenerReportesFiltrados(string cliente, DateTime? desde, DateTime? hasta)
        {
            var query = _context.Ventas
                .Include(v => v.IdClienteNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(cliente))
                query = query.Where(v => v.IdClienteNavigation.Nombre.Contains(cliente));

            if (desde.HasValue)
                query = query.Where(v => v.Fecha >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(v => v.Fecha <= hasta.Value);

            return await query.Select(v => new Reporte
            {
                IdVenta = v.IdVenta,
                NombreCliente = v.IdClienteNavigation.Nombre,
                Subtotal = v.Subtotal,
                Impuestos = v.Impuestos,
                Total = v.Total,
                FechaVenta = v.Fecha ?? DateTime.Now
            }).ToListAsync();
        }

        // NUEVO MÉTODO AUXILIAR para obtener reportes de Mantenimientos
        private async Task<List<ReporteMantenimiento>> ObtenerReportesMantenimientosFiltrados(string cliente, DateTime? desde, DateTime? hasta)
        {
            var query = _context.Mantenimientos
                .Include(m => m.Cliente)
                .AsQueryable();

            if (!string.IsNullOrEmpty(cliente))
                query = query.Where(m => m.Cliente.Nombre.Contains(cliente));

            if (desde.HasValue)
                query = query.Where(m => m.FechaProgramada >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(m => m.FechaProgramada <= hasta.Value);

            return await query
                .Select(m => new ReporteMantenimiento
                {
                    IdMantenimiento = m.IdMantenimiento,
                    NombreCliente = m.Cliente.Nombre,
                    TipoMantenimiento = m.TipoMantenimiento,
                    Fecha = m.FechaProgramada ?? DateTime.Now,
                    CostoTotal = m.CostoTotal ?? 0
                })
                .ToListAsync();
        }


        // **********************************************
        // ACCIONES DE EXPORTACIÓN (VENTAS)
        // **********************************************

        public async Task<IActionResult> ExportarExcel(string cliente, DateTime? desde, DateTime? hasta)
        {
            var reportes = await ObtenerReportesFiltrados(cliente, desde, hasta);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("ReporteVentas");

            // Cabeceras
            ws.Cells[1, 1].Value = "Cliente";
            ws.Cells[1, 2].Value = "Subtotal";
            ws.Cells[1, 3].Value = "Impuestos";
            ws.Cells[1, 4].Value = "Total";
            ws.Cells[1, 5].Value = "Fecha";

            // Datos
            for (int i = 0; i < reportes.Count; i++)
            {
                var r = reportes[i];
                ws.Cells[i + 2, 1].Value = r.NombreCliente;
                ws.Cells[i + 2, 2].Value = r.Subtotal;
                ws.Cells[i + 2, 3].Value = r.Impuestos;
                ws.Cells[i + 2, 4].Value = r.Total;
                ws.Cells[i + 2, 5].Value = r.FechaVenta.ToString("dd/MM/yyyy");

                // Formato de moneda para Subtotal, Impuestos y Total
                ws.Cells[i + 2, 2].Style.Numberformat.Format = "_(\"S/\"* #,##0.00_);_(\"S/\"* (#,##0.00);_(\"S/\"* \"-\"??_);_(@_)";
                ws.Cells[i + 2, 3].Style.Numberformat.Format = "_(\"S/\"* #,##0.00_);_(\"S/\"* (#,##0.00);_(\"S/\"* \"-\"??_);_(@_)";
                ws.Cells[i + 2, 4].Style.Numberformat.Format = "_(\"S/\"* #,##0.00_);_(\"S/\"* (#,##0.00);_(\"S/\"* \"-\"??_);_(@_)";
            }

            ws.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReporteVentas.xlsx");
        }


        public async Task<IActionResult> ExportarPDF(string cliente, DateTime? desde, DateTime? hasta)
        {
            var reportes = await ObtenerReportesFiltrados(cliente, desde, hasta);

            using var stream = new MemoryStream();
            var doc = new iTextSharp.text.Document(PageSize.A4, 25, 25, 30, 30);
            PdfWriter.GetInstance(doc, stream);
            doc.Open();

            // Título
            doc.Add(new Paragraph("Reporte de Ventas", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14)));
            doc.Add(new Chunk("\n"));

            var table = new PdfPTable(5) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 3f, 2f, 2f, 2f, 2f });

            // Cabeceras
            table.AddCell(new Phrase("Cliente", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
            table.AddCell(new Phrase("Subtotal", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
            table.AddCell(new Phrase("Impuestos", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
            table.AddCell(new Phrase("Total", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
            table.AddCell(new Phrase("Fecha", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));

            // Datos
            foreach (var r in reportes)
            {
                table.AddCell(r.NombreCliente);
                table.AddCell($"S/ {r.Subtotal.Value.ToString("F2")}");
                table.AddCell($"S/ {r.Impuestos.Value.ToString("F2")}");
                table.AddCell($"S/ {r.Total.Value.ToString("F2")}");
                table.AddCell(r.FechaVenta.ToString("dd/MM/yyyy"));
            }

            doc.Add(table);
            doc.Close();

            var bytes = stream.ToArray();
            return File(bytes, "application/pdf", "ReporteVentas.pdf");
        }

        // **********************************************
        // ACCIONES DE EXPORTACIÓN (MANTENIMIENTOS) - NUEVAS
        // **********************************************

        public async Task<IActionResult> ExportarExcelMantenimientos(string cliente, DateTime? desde, DateTime? hasta)
        {
            var reportes = await ObtenerReportesMantenimientosFiltrados(cliente, desde, hasta);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("ReporteMantenimientos");

            // Cabeceras
            ws.Cells[1, 1].Value = "Cliente";
            ws.Cells[1, 2].Value = "Tipo de Mantenimiento";
            ws.Cells[1, 3].Value = "Fecha";
            ws.Cells[1, 4].Value = "Costo Total";

            // Estilo para la cabecera (Opcional, mejora visual)
            using (var range = ws.Cells[1, 1, 1, 4])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Datos
            for (int i = 0; i < reportes.Count; i++)
            {
                var r = reportes[i];
                ws.Cells[i + 2, 1].Value = r.NombreCliente;
                ws.Cells[i + 2, 2].Value = r.TipoMantenimiento;
                ws.Cells[i + 2, 3].Value = r.Fecha.ToString("dd/MM/yyyy");
                ws.Cells[i + 2, 4].Value = r.CostoTotal;

                // Formato de moneda para Costo Total
                ws.Cells[i + 2, 4].Style.Numberformat.Format = "_(\"S/\"* #,##0.00_);_(\"S/\"* (#,##0.00);_(\"S/\"* \"-\"??_);_(@_)";
            }

            ws.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReporteMantenimientos.xlsx");
        }

        public async Task<IActionResult> ExportarPDFMantenimientos(string cliente, DateTime? desde, DateTime? hasta)
        {
            var reportes = await ObtenerReportesMantenimientosFiltrados(cliente, desde, hasta);

            using var stream = new MemoryStream();
            var doc = new iTextSharp.text.Document(PageSize.A4, 25, 25, 30, 30);
            PdfWriter.GetInstance(doc, stream);
            doc.Open();

            // Título
            doc.Add(new Paragraph("Reporte de Mantenimientos", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14)));
            doc.Add(new Chunk("\n"));

            var table = new PdfPTable(4) { WidthPercentage = 100 }; // 4 columnas
            table.SetWidths(new float[] { 3f, 3f, 2f, 2f });

            // Cabeceras
            table.AddCell(new Phrase("Cliente", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
            table.AddCell(new Phrase("Tipo de Mantenimiento", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
            table.AddCell(new Phrase("Fecha", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
            table.AddCell(new Phrase("Costo Total", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));

            // Datos
            foreach (var r in reportes)
            {
                table.AddCell(r.NombreCliente);
                table.AddCell(r.TipoMantenimiento);
                table.AddCell(r.Fecha.ToString("dd/MM/yyyy"));
                // Formato de moneda, asegurando que CostoTotal tenga un valor (ya está garantizado por el ?? 0 en el DTO)
                table.AddCell($"S/ {r.CostoTotal.Value.ToString("F2")}");
            }

            doc.Add(table);
            doc.Close();

            var bytes = stream.ToArray();
            return File(bytes, "application/pdf", "ReporteMantenimientos.pdf");
        }

        // La acción ReporteMantenimientos no se usa directamente en la vista Index, 
        // pero se mantiene por si es usada en otra parte. Si no se usa, puede eliminarse.
        public async Task<IActionResult> ReporteMantenimientos(string cliente, DateTime? desde, DateTime? hasta)
        {
            var reportes = await ObtenerReportesMantenimientosFiltrados(cliente, desde, hasta);
            return View(reportes);
        }
    }
}