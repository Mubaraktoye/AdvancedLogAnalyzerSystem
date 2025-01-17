namespace Infrastructure.Models.DTOs;

public record DateRange
{
    public string DirectoryLoc { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
