using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.EntityFrameworkCore;
using MediVerify.Data;
using MediVerify.Models;

namespace MediVerify.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        public ReportService(ApplicationDbContext context) => _context = context;

        public async Task<byte[]> GeneratePdfReportAsync()
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Items).ThenInclude(i => i.Medicine)
                .ToListAsync();

            using var ms = new MemoryStream();
            var doc = new Document(PageSize.A4);
            var writer = PdfWriter.GetInstance(doc, ms);
            doc.Open();
            var title = new Paragraph("MediVerify Prescription Report")
            { Alignment = Element.ALIGN_CENTER, SpacingAfter = 20f };
            doc.Add(title);

            foreach (var p in prescriptions)
            {
                doc.Add(new Paragraph($"Prescription #{p.PrescriptionNumber} - {p.CreatedAt:u}"));
                foreach (var item in p.Items ?? Enumerable.Empty<PrescriptionItem>())
                {
                    var medicineName = item.Medicine?.MedicineName ?? "Unknown medicine";
                    doc.Add(new Paragraph($"  • {medicineName} - Qty: {item.Quantity} - {item.PrescribedInstructions}"));
                }
                doc.Add(new Chunk("\n"));
            }

            doc.Close();
            return ms.ToArray();
        }

        public async Task<byte[]> GenerateExcelReportAsync()
        {
            var data = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Items).ThenInclude(i => i.Medicine)
                .SelectMany(p => p.Items, (p, i) => new
                {
                    PrescriptionNumber = p.PrescriptionNumber,
                    Patient = p.Patient == null || p.Patient.ApplicationUser == null ? "Unknown patient" : p.Patient.ApplicationUser.FullName,
                    Medicine = i.Medicine == null ? "Unknown medicine" : i.Medicine.MedicineName,
                    Quantity = i.Quantity,
                    Date = p.PrescriptionDate
                })
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Prescriptions");
            ws.Cell(1, 1).Value = "PrescriptionNumber";
            ws.Cell(1, 2).Value = "Patient";
            ws.Cell(1, 3).Value = "Medicine";
            ws.Cell(1, 4).Value = "Quantity";
            ws.Cell(1, 5).Value = "Date";
            int row = 2;
            foreach (var r in data)
            {
                ws.Cell(row, 1).Value = r.PrescriptionNumber;
                ws.Cell(row, 2).Value = r.Patient;
                ws.Cell(row, 3).Value = r.Medicine;
                ws.Cell(row, 4).Value = r.Quantity;
                ws.Cell(row, 5).Value = r.Date;
                row++;
            }
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        public async Task<string> GenerateCsvReportAsync()
        {
            var sb = new StringBuilder();
            sb.AppendLine("PrescriptionNumber,Patient,Medicine,Quantity,Date");
            var data = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Items).ThenInclude(i => i.Medicine)
                .SelectMany(p => p.Items, (p, i) => new
                {
                    PrescriptionNumber = p.PrescriptionNumber,
                    Patient = p.Patient == null || p.Patient.ApplicationUser == null ? "Unknown patient" : p.Patient.ApplicationUser.FullName,
                    Medicine = i.Medicine == null ? "Unknown medicine" : i.Medicine.MedicineName,
                    Quantity = i.Quantity,
                    Date = p.PrescriptionDate
                })
                .ToListAsync();
            foreach (var r in data)
            {
                sb.AppendLine(string.Join(",",
                    EscapeCsv(r.PrescriptionNumber),
                    EscapeCsv(r.Patient),
                    EscapeCsv(r.Medicine),
                    r.Quantity,
                    r.Date.ToString("u")));
            }
            return sb.ToString();
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }
    }
}
