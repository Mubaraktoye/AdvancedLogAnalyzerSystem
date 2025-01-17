namespace Infrastructure.Models.DTOs;

public record UploadRequest
{
    public string DirectoryLoc { get; set; }
    public string ServerUrl { get; set; }
}
