using Infrastructure.Models.DTOs;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Host.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LogsManagerController : ControllerBase
{
    private readonly ILoggersService _logsService;

    public LogsManagerController(ILoggersService logsService)
    {
        _logsService = logsService;
    }

    /// <summary>
    /// Counts the number of unique errors in the logs within the specified file path.
    /// </summary>
    /// <param name="filePath">The path to the log file or directory.</param>
    /// <returns>A count of unique errors and the execution time in milliseconds.</returns>
    [HttpGet("errors/unique")]
    public async Task<IActionResult> CountUniqueErrors(string filePath)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var count = await _logsService.CountUniqueErrorsPerLogAsync(filePath);
            stopwatch.Stop();

            return Ok(new
            {
                Count = count,
                ExecutionTimeInMilliseconds = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Error = ex.Message,
                ExecutionTimeInMilliseconds = 0
            });
        }
    }

    /// <summary>
    /// Counts the number of duplicate errors in the logs within the specified file path.
    /// </summary>
    /// <param name="filePath">The path to the log file or directory.</param>
    /// <returns>A count of duplicate errors and the execution time in milliseconds.</returns>
    [HttpGet("errors/duplicates")]
    public async Task<IActionResult> CountDuplicateErrors(string filePath)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var count = await _logsService.CountDuplicateErrorsPerLogAsync(filePath);
            stopwatch.Stop();

            return Ok(new
            {
                Count = count,
                ExecutionTimeInMilliseconds = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Error = ex.Message,
                ExecutionTimeInMilliseconds = 0
            });
        }
    }

    /// <summary>
    /// Uploads all logs in the specified directory to a remote server.
    /// </summary>
    /// <param name="remoteApiUrl">The API URL of the remote server.</param>
    /// <param name="directoryPath">The path to the directory containing the logs.</param>
    /// <returns>Confirmation message and execution time in milliseconds.</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadLogsToServer(string remoteApiUrl, string directoryPath)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            await _logsService.UploadLogsAsync(remoteApiUrl, directoryPath);
            stopwatch.Stop();

            return Ok(new
            {
                Message = "Logs uploaded successfully.",
                ExecutionTimeInMilliseconds = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Error = ex.Message,
                ExecutionTimeInMilliseconds = 0
            });
        }
    }

    /// <summary>
    /// Deletes archived logs within the specified date range.
    /// </summary>
    /// <param name="dateRange">The date range for which to delete archives.</param>
    /// <returns>Confirmation message and execution time in milliseconds.</returns>
    [HttpDelete("archive")]
    public IActionResult DeleteArchiveFromPeriod(DateRange dateRange)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            _logsService.DeleteLogsByPeriod(dateRange);
            stopwatch.Stop();

            return Ok(new
            {
                Message = "Archives deleted successfully.",
                ExecutionTimeInMilliseconds = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Error = ex.Message,
                ExecutionTimeInMilliseconds = 0
            });
        }
    }

    /// <summary>
    /// Archives logs within the specified date range by compressing them into a zip file.
    /// </summary>
    /// <param name="dateRange">The date range for which to archive logs.</param>
    /// <returns>Confirmation message and execution time in milliseconds.</returns>
    [HttpPost("archive")]
    public IActionResult ArchiveLogsFromPeriod(DateRange dateRange)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            _logsService.ArchiveLogs(dateRange);
            stopwatch.Stop();

            return Ok(new
            {
                Message = "Logs archived successfully.",
                ExecutionTimeInMilliseconds = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Error = ex.Message,
                ExecutionTimeInMilliseconds = 0
            });
        }
    }

    /// <summary>
    /// Searches for logs within the specified size range.
    /// </summary>
    /// <param name="sizeRange">The size range for the logs to search.</param>
    /// <returns>A list of log file paths and execution time in milliseconds.</returns>
    [HttpGet("logs/size")]
    public IActionResult SearchLogsBySize(SizeRange sizeRange)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var logs = _logsService.SearchLogsBySize(sizeRange);
            stopwatch.Stop();

            return Ok(new
            {
                Logs = logs,
                ExecutionTimeInMilliseconds = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Error = ex.Message,
                ExecutionTimeInMilliseconds = 0
            });
        }
    }

    /// <summary>
    /// Searches for logs within the specified directory.
    /// </summary>
    /// <param name="directoryPath">The directory path to search for logs.</param>
    /// <returns>A list of log file paths and execution time in milliseconds.</returns>
    [HttpGet("logs/directory")]
    public IActionResult SearchLogsInDirectory(string directoryPath)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var logs = _logsService.SearchLogsByDirectory(directoryPath);
            stopwatch.Stop();

            return Ok(new
            {
                Logs = logs,
                ExecutionTimeInMilliseconds = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Error = ex.Message,
                ExecutionTimeInMilliseconds = 0
            });
        }
    }
}
