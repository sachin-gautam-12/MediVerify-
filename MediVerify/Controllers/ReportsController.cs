using System;
using System.Text;
using System.Threading.Tasks;
using MediVerify.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediVerify.Controllers
{
    [Authorize(Policy = "AdminPolicy")]
    public class ReportsController : Controller
    {
        private readonly IReportService _reports;

        public ReportsController(IReportService reports) => _reports = reports;

        public IActionResult Index() => View();

        public async Task<IActionResult> Pdf()
        {
            var bytes = await _reports.GeneratePdfReportAsync();
            return File(bytes, "application/pdf", $"mediverify-prescriptions-{DateTime.UtcNow:yyyyMMdd}.pdf");
        }

        public async Task<IActionResult> Excel()
        {
            var bytes = await _reports.GenerateExcelReportAsync();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"mediverify-prescriptions-{DateTime.UtcNow:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> Csv()
        {
            var csv = await _reports.GenerateCsvReportAsync();
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"mediverify-prescriptions-{DateTime.UtcNow:yyyyMMdd}.csv");
        }
    }
}
