namespace GPXConverter.Core.Models;

/// <summary>
/// Class representing a point for a height profile
/// </summary>
public class ElevationPoint
{
    /// <summary>
    /// Distance from the start (in meters)
    /// </summary>
    public double Distance { get; set; }
    
    /// <summary>
    /// Height above sea level (in meters)
    /// </summary>
    public double Elevation { get; set; }
    
    /// <summary>
    /// Slope at the point (in percent)
    /// </summary>
    public double Grade { get; set; }
}