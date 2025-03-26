namespace GPXConverter.Core.Models;

/// <summary>
/// Class containing the results of the analysis of GPS data
/// </summary>
public class GpsAnalysisResult
{
    /// <summary>
    /// Total distance (in meters)
    /// </summary>
    public double TotalDistance { get; set; }
    
    /// <summary>
    /// Total duration
    /// </summary>
    public TimeSpan TotalTime { get; set; }
    
    /// <summary>
    /// Time in motion (excluding pauses)
    /// </summary>
    public TimeSpan MovingTime { get; set; }
    
    /// <summary>
    /// Average speed (in m/s)
    /// </summary>
    public double AverageSpeed { get; set; }
    
    /// <summary>
    /// Maximum speed (in m/s)
    /// </summary>
    public double MaxSpeed { get; set; }
    
    /// <summary>
    /// Total ascent (in meters)
    /// </summary>
    public double TotalAscent { get; set; }
    
    /// <summary>
    /// Total descent (in meters)
    /// </summary>
    public double TotalDescent { get; set; }
    
    /// <summary>
    /// Minimum height (in meters)
    /// </summary>
    public double MinElevation { get; set; }
    
    /// <summary>
    /// Maximum height (in meters)
    /// </summary>
    public double MaxElevation { get; set; }
    
    /// <summary>
    /// Average slope (in percent)
    /// </summary>
    public double AverageGrade { get; set; }
    
    /// <summary>
    /// Maximum slope (in percent)
    /// </summary>
    public double MaxGrade { get; set; }

    /// <summary>
    /// Height profile points
    /// </summary>
    public List<ElevationPoint> ElevationProfile { get; set; } = [];
}