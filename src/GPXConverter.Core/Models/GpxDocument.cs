namespace GPXConverter.Core.Models;

/// <summary>
/// Master class representing the entire contents of a GPX file
/// </summary>
public class GpxDocument
{
    public GpxMetadata? Metadata { get; set; }
    
    public List<Waypoint> Waypoints { get; set; } = [];
    
    public List<Route> Routes { get; set; } = [];
    
    public List<Track> Tracks { get; set; } = [];
}