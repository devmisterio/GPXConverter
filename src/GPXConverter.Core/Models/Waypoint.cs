namespace GPXConverter.Core.Models;

/// <summary>
/// Represents a point (waypoint) in the GPX file
/// </summary>
public class Waypoint
{
    public double Latitude { get; set; }
    
    public double Longitude { get; set; }
    
    public double? Elevation { get; set; }
    
    public DateTime? Time { get; set; }
    
    public string? Name { get; set; }
    
    public string? Description { get; set; }
    
    public string? Symbol { get; set; }
    
    public Dictionary<string, string>? Extensions { get; set; }
}