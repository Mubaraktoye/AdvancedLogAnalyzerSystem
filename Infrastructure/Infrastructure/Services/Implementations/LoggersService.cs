using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Infrastructure.Models.DTOs;
using Infrastructure.Services.Interfaces;

namespace Infrastructure.Services.Implementations;

public class LoggersService : ILoggersService
{

    #region Counting Errors

    private static List<string> RetrieveLogFilePaths(string folderPath)
    {
        try
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"The directory '{folderPath}' does not exist.");

            return Directory.EnumerateFiles(folderPath, "*.log", SearchOption.AllDirectories).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return [];
        }
    }

    private static readonly Regex ErrorRegex = new Regex(
        @"\d{2}\.\d{2}\.\d{4} \d{2}:\d{2}:\d{2}(?::\d{4})? .*?: (.*)",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static string? ExtractError(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var match = ErrorRegex.Match(line);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }


    #endregion

    #region US-10: Count Total available logs in a period
    public int CountLogsInPeriod(DateRange range)
    {
        return Directory.GetFiles(range.DirectoryLoc, "*.log", SearchOption.AllDirectories)
            .Count(file => FileNameMatchesDateRange(file, range));
    }

    private static bool FileNameMatchesDateRange(string file, DateRange range)
    {
        var fileName = Path.GetFileNameWithoutExtension(file);
        if (fileName == null)
            return false;

        var datePart = fileName.Split('_').FirstOrDefault();
        if (DateTime.TryParseExact(datePart, "yyyy.MM.dd", null, DateTimeStyles.None, out var fileDate))
            return fileDate >= range.StartDate && fileDate <= range.EndDate;

        return false;
    }
    #endregion

    #region US-11: Delete logs from a period
    public void DeleteLogsByPeriod(DateRange range)
    {
        var logsToDelete = Directory.GetFiles(range.DirectoryLoc, "*.log", SearchOption.AllDirectories)
            .Where(file => FileNameMatchesDateRange(file, range));

        foreach (var log in logsToDelete)
            File.Delete(log);
    }
    #endregion

    #region US-12 : Counts number of unique errors per log files
    public async Task<Dictionary<string, int>> CountUniqueErrorsPerLogAsync(string folderPath)
    {
        var fileCount = new ConcurrentDictionary<string, int>();
        var filePaths = RetrieveLogFilePaths(folderPath);

        var tasks = filePaths.Select(async file =>
        {
            try
            {
                var distinctErrors = new HashSet<string>(); // Use HashSet for O(1) lookup
                using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var bufferedStream = new BufferedStream(fileStream);
                using var streamReader = new StreamReader(bufferedStream);

                var batch = new List<string>();
                while (!streamReader.EndOfStream)
                {
                    batch.Clear();

                    // Read lines in batches to reduce I/O overhead
                    for (int i = 0; i < 1000 && !streamReader.EndOfStream; i++)
                    {
                        var line = await streamReader.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(line))
                            batch.Add(line);
                    }

                    // Process the batch of lines
                    foreach (var line in batch)
                    {
                        var error = ExtractError(line);
                        if (error != null)
                        {
                            distinctErrors.Add(error); // HashSet ensures uniqueness
                        }
                    }
                }

                // Add unique error count for this file
                fileCount[file] = distinctErrors.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {file}: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks); // Wait for all file tasks to complete

        return new Dictionary<string, int>(fileCount);
    }

    #endregion

    #region US-13: Counts number of duplicated errors per log files
    public async Task<Dictionary<string, int>> CountDuplicateErrorsPerLogAsync(string folderPath)
    {
        var fileDuplicateCounts = new ConcurrentDictionary<string, int>();
        var filePaths = RetrieveLogFilePaths(folderPath);

        var tasks = filePaths.Select(async file =>
        {
            try
            {
                var errorCounts = new Dictionary<string, int>();
                using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var bufferedStream = new BufferedStream(fileStream);
                using var streamReader = new StreamReader(bufferedStream);

                var batch = new List<string>();
                while (!streamReader.EndOfStream)
                {
                    batch.Clear();

                    // Read lines in batches to reduce I/O overhead
                    for (int i = 0; i < 1000 && !streamReader.EndOfStream; i++)
                    {
                        var line = await streamReader.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(line)) batch.Add(line);
                    }

                    // Process the batch of lines
                    foreach (var line in batch)
                    {
                        var error = ExtractError(line);
                        if (error != null)
                        {
                            if (errorCounts.ContainsKey(error))
                                errorCounts[error]++;
                            else
                                errorCounts[error] = 1;
                        }
                    }
                }

                // Calculate duplicate errors (count > 1)
                var duplicates = errorCounts.Values.Where(count => count > 1).Sum(count => count - 1); // Only count duplicates
                fileDuplicateCounts[file] = duplicates;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {file}: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks); // Wait for all file tasks to complete

        return new Dictionary<string, int>(fileDuplicateCounts);
    }


    #endregion

    #region US-14: Search logs per directory
    public List<string> SearchLogsByDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"The directory '{directory}' does not exist.");

        return RetrieveLogFilePaths(directory);
    }
    #endregion

    #region US-15: Search logs per size
    public List<string> SearchLogsBySize(SizeRange range)
    {
        return RetrieveLogFilePaths(range.DirectoryLoc)
            .Where(file =>
            {
                var sizeInKb = new FileInfo(file).Length / 1024;
                return sizeInKb >= range.MinSizeKb && sizeInKb <= range.MaxSizeKb;
            })
            .ToList();
    }

    #endregion

    #region US-16: archive logs from a period
    public void ArchiveLogs(DateRange range)
    {
        var logsToArchive = RetrieveLogFilePaths(range.DirectoryLoc)
            .Where(file => FileNameMatchesDateRange(file, range))
            .ToList();

        if (!logsToArchive.Any()) return;

        var zipFileName = Path.Combine(range.DirectoryLoc, $"{range.StartDate:dd_MM_yyyy}-{range.EndDate:dd_MM_yyyy}.zip");

        using var zip = ZipFile.Open(zipFileName, ZipArchiveMode.Create);
        foreach (var log in logsToArchive)
        {
            zip.CreateEntryFromFile(log, Path.GetFileName(log));
            File.Delete(log);
        }
    }
    #endregion

    #region US-17: Delete archive from a period
    public void DeleteArchives(DateRange range)
    {
        var files = RetrieveLogFilePaths(range.DirectoryLoc);

        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (TryParseDateRange(fileName, out var fileRange) &&
                fileRange.StartDate >= range.StartDate &&
                fileRange.EndDate <= range.EndDate)
            {
                File.Delete(file);
            }
        }
    }

    private static bool TryParseDateRange(string fileName, out DateRange? range)
    {
        var parts = fileName.Split('-');
        if (parts.Length == 2 &&
            DateTime.TryParseExact(parts[0], "dd_MM_yyyy", null, DateTimeStyles.None, out var startDate) &&
            DateTime.TryParseExact(parts[1], "dd_MM_yyyy", null, DateTimeStyles.None, out var endDate))
        {
            range = new DateRange { StartDate = startDate, EndDate = endDate };
            return true;
        }

        range = null;
        return false;
    }
    #endregion

    #region  US-18: upload logs on a remote server per API
    public async Task UploadLogsAsync(string filePath, string serverUrl)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"The file '{filePath}' does not exist.");

        using var client = new HttpClient();
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var content = new MultipartFormDataContent
        {
            { new StreamContent(fileStream), "file", Path.GetFileName(filePath) }
        };

        var response = await client.PostAsync(serverUrl, content);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to upload log file. Status code: {response.StatusCode}");
        }
    }
    #endregion

    #region US-18: Upload logs on a remote server per API
    public async Task UploadLogsAsync(IEnumerable<string> filePaths, string serverUrl)
    {
        if (filePaths == null || !filePaths.Any())
            throw new ArgumentException("No files provided for upload.", nameof(filePaths));

        using var client = new HttpClient();

        // Create tasks for uploading each file
        var uploadTasks = filePaths.Select(async filePath =>
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The file '{filePath}' does not exist.");

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var content = new MultipartFormDataContent
        {
            { new StreamContent(fileStream), "file", Path.GetFileName(filePath) }
        };

            var response = await client.PostAsync(serverUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to upload file '{filePath}'. Status code: {response.StatusCode}");
            }
        });

        // Wait for all uploads to complete
        await Task.WhenAll(uploadTasks);
    }
    #endregion

}
