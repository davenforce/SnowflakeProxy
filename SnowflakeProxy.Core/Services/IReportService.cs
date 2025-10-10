using System.Data;
using SnowflakeProxy.Core.Models;

namespace SnowflakeProxy.Core.Services;

public interface IReportService
{
    Task<ReportResult> GenerateReportAsync(ReportConfig config, CancellationToken cancellationToken = default);
    Task<ReportResult> GenerateReportAsync(string reportId, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
}