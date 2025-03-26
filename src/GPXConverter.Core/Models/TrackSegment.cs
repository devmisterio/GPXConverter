namespace GPXConverter.Core.Models;

/// <summary>
/// Represents a continuous index of points in a GPS track
/// </summary>
public class TrackSegment
{
    public List<Waypoint> TrackPoints { get; set; } = [];
}