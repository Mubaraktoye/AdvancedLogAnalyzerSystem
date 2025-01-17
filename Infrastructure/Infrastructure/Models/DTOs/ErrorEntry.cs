namespace Infrastructure.Models.DTOs;

public record ErrorEntry
{
    public DateTime Timestamp { get; set; }
    public string ErrorMessage { get; set; }
}
