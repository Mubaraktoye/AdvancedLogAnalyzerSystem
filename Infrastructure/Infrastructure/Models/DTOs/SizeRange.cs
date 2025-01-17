namespace Infrastructure.Models.DTOs;

public record SizeRange
{
    public string DirectoryLoc { get; set; }
    public int MinSizeKb { get; set; }
    public int MaxSizeKb { get; set; }
}