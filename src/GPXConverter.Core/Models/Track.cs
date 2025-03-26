namespace GPXConverter.Core.Models;

/// <summary>
/// GPS track may contain one or more segments
/// </summary>
public class Track
{
    public string? Name { get; set; }
    
    public string? Description { get; set; }
    
    public List<TrackSegment> Segments { get; set; } = [];
}