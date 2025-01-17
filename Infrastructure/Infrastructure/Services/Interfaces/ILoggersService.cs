using Infrastructure.Models.DTOs;

namespace Infrastructure.Services.Interfaces
{
    public interface ILoggersService
    {
        void ArchiveLogs(DateRange range);
        Task<Dictionary<string, int>> CountDuplicateErrorsPerLogAsync(string folderPath);
        int CountLogsInPeriod(DateRange range);
        Task<Dictionary<string, int>> CountUniqueErrorsPerLogAsync(string folderPath);
        void DeleteArchives(DateRange range);
        void DeleteLogsByPeriod(DateRange range);
        List<string> SearchLogsByDirectory(string directory);
        List<string> SearchLogsBySize(SizeRange range);
        Task UploadLogsAsync(string filePath, string serverUrl);
    }
}