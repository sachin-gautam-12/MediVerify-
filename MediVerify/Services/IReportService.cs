using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediVerify.Services
{
    public interface IReportService
    {
        Task<byte[]> GeneratePdfReportAsync();
        Task<byte[]> GenerateExcelReportAsync();
        Task<string> GenerateCsvReportAsync();
    }
}
