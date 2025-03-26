namespace GPXConverter.Core.Models;

/// <summary>
/// A route - represents the targeted journey
/// </summary>
public class Route
{
    public string? Name { get; set; }
    
    public string? Description { get; set; }
    
    public List<Waypoint> RoutePoints { get; set; } = [];
}