namespace GPXConverter.Core.Models;

/// <summary>
/// GPX file metadata
/// </summary>
public class GpxMetadata
{
    public string? Name { get; set; }
    
    public string? Description { get; set; }
    
    public string? Author { get; set; }
    
    public DateTime? Time { get; set; }
    
    public List<string> Keywords { get; set; } = [];
}