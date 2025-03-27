using GPXConverter.Core.Interfaces;
using GPXConverter.Core.Models;
using Microsoft.Extensions.Logging;

namespace GPXConverter.Infrastructure.Services;

/// <summary>
/// Service for analyzing GPS data and calculating statistics
/// </summary>
public class GpsAnalyzer : IGpsAnalyzer
{
    private readonly IGpxReader _gpxReader;
    private readonly ILogger<GpsAnalyzer> _logger;
    private const double EarthRadiusMeters = 6371000.0; // Earth radius in meters

    /// <summary>
    /// Initializes a new instance of the GpsAnalyzer class
    /// </summary>
    /// <param name="gpxReader">GPX reader for loading files</param>
    /// <param name="logger">Logger instance</param>
    public GpsAnalyzer(IGpxReader gpxReader, ILogger<GpsAnalyzer> logger)
    {
        _gpxReader = gpxReader;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<GpsAnalysisResult> AnalyzeAsync(GpxDocument document, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing GPX document with {TrackCount} tracks, {RouteCount} routes, and {WaypointCount} waypoints", 
            document.Tracks.Count, document.Routes.Count, document.Waypoints.Count);

        try
        {
            var result = new GpsAnalysisResult();
            
            foreach (var track in document.Tracks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var segment in track.Segments.Where(segment => segment.TrackPoints.Count >= 2))
                {
                    AnalyzeTrackSegment(segment, result);
                }
            }
            
            foreach (var route in document.Routes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                if (route.RoutePoints.Count < 2) continue;
                
                AnalyzeRoute(route, result);
            }

            // If we have elevation data, generate a simplified elevation profile
            if (result.TotalDistance > 0 && result.MaxElevation > result.MinElevation)
            {
                result.ElevationProfile = GenerateElevationProfile(document, 100); // 100 points for profile
            }
            
            _logger.LogInformation("Analysis completed. Total distance: {TotalDistance:F2} km", result.TotalDistance / 1000);
            
            return Task.FromResult(result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error analyzing GPX document");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<GpsAnalysisResult> AnalyzeFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing GPX file: {FilePath}", filePath);
        
        try
        {
            var document = await _gpxReader.ReadAsync(filePath, cancellationToken);
            return await AnalyzeAsync(document, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error analyzing GPX file: {FilePath}", filePath);
            throw;
        }
    }

    private void AnalyzeTrackSegment(TrackSegment segment, GpsAnalysisResult result)
    {
        var points = segment.TrackPoints;
        
        // Calculate distance metrics
        double segmentDistance = CalculateDistance(points);
        result.TotalDistance += segmentDistance;
        
        // Calculate time metrics if time data is available
        if (points.All(p => p.Time.HasValue))
        {
            CalculateTimeMetrics(points, result);
        }
        
        // Calculate elevation metrics if elevation data is available
        if (points.All(p => p.Elevation.HasValue))
        {
            CalculateElevationMetrics(points, result);
        }
        
        // Calculate speed metrics if both time and distance are available
        if (points.All(p => p.Time.HasValue) && segmentDistance > 0)
        {
            CalculateSpeedMetrics(points, result);
        }
    }

    private void AnalyzeRoute(Route route, GpsAnalysisResult result)
    {
        var points = route.RoutePoints;
        
        // Calculate distance metrics
        double routeDistance = CalculateDistance(points);
        result.TotalDistance += routeDistance;
        
        // Calculate time metrics if time data is available
        if (points.All(p => p.Time.HasValue))
        {
            CalculateTimeMetrics(points, result);
        }
        
        // Calculate elevation metrics if elevation data is available
        if (points.All(p => p.Elevation.HasValue))
        {
            CalculateElevationMetrics(points, result);
        }
        
        // Calculate speed metrics if both time and distance are available
        if (points.All(p => p.Time.HasValue) && routeDistance > 0)
        {
            CalculateSpeedMetrics(points, result);
        }
    }

    private double CalculateDistance(IList<Waypoint> points)
    {
        double totalDistance = 0;
        
        for (int i = 0; i < points.Count - 1; i++)
        {
            var p1 = points[i];
            var p2 = points[i + 1];
            
            totalDistance += CalculateHaversineDistance(
                p1.Latitude, p1.Longitude, 
                p2.Latitude, p2.Longitude);
        }
        
        return totalDistance;
    }

    private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Convert degrees to radians
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);
        lat1 = ToRadians(lat1);
        lat2 = ToRadians(lat2);
        
        // Haversine formula
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * 
                   Math.Cos(lat1) * Math.Cos(lat2);
        
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return EarthRadiusMeters * c; // Distance in meters
    }

    private void CalculateTimeMetrics(IList<Waypoint> points, GpsAnalysisResult result)
    {
        var times = points.Select(p => p.Time!.Value).OrderBy(t => t).ToList();
        var totalDuration = times.Last() - times.First();
        
        // Update total time if this is longer than current total
        if (result.TotalTime == TimeSpan.Zero || totalDuration > result.TotalTime)
        {
            result.TotalTime = totalDuration;
        }
        
        // Calculate moving time (exclude stops)
        TimeSpan movingTime = TimeSpan.Zero;
        const double speedThreshold = 0.5; // m/s, below this is considered stopped
        
        for (int i = 0; i < points.Count - 1; i++)
        {
            var p1 = points[i];
            var p2 = points[i + 1];
            
            if (!p1.Time.HasValue || !p2.Time.HasValue)
                continue;
            
            var timeDiff = p2.Time.Value - p1.Time.Value;
            if (timeDiff.TotalSeconds <= 0)
                continue; // Skip invalid time intervals
                
            var distance = CalculateHaversineDistance(p1.Latitude, p1.Longitude, p2.Latitude, p2.Longitude);
            var speed = distance / timeDiff.TotalSeconds; // m/s
            
            if (speed >= speedThreshold)
            {
                movingTime += timeDiff;
            }
        }
        
        result.MovingTime += movingTime;
    }

    private void CalculateElevationMetrics(IList<Waypoint> points, GpsAnalysisResult result)
    {
        double ascent = 0;
        double descent = 0;
        double minElevation = double.MaxValue;
        double maxElevation = double.MinValue;
        
        for (int i = 0; i < points.Count; i++)
        {
            var elevation = points[i].Elevation ?? 0;
            
            // Update min/max elevations
            if (elevation < minElevation) minElevation = elevation;
            if (elevation > maxElevation) maxElevation = elevation;
            
            // Calculate ascent/descent
            if (i > 0)
            {
                var prevElevation = points[i - 1].Elevation ?? 0;
                var diff = elevation - prevElevation;
                
                // Filter out small elevation changes that might be noise
                const double noiseThreshold = 1.0; // 1 meter
                
                if (diff > noiseThreshold)
                {
                    ascent += diff;
                }
                else if (diff < -noiseThreshold)
                {
                    descent += Math.Abs(diff);
                }
            }
        }
        
        // Update result
        if (result.MinElevation == 0 || minElevation < result.MinElevation)
            result.MinElevation = minElevation;
            
        if (maxElevation > result.MaxElevation)
            result.MaxElevation = maxElevation;
            
        result.TotalAscent += ascent;
        result.TotalDescent += descent;
        
        // Calculate average grade if we have distance
        if (!(result.TotalDistance > 0)) return;
        
        double elevationChange = maxElevation - minElevation;
        result.AverageGrade = (elevationChange / result.TotalDistance) * 100; // As percentage
    }

    private void CalculateSpeedMetrics(IList<Waypoint> points, GpsAnalysisResult result)
    {
        List<double> speeds = new();
        
        for (int i = 0; i < points.Count - 1; i++)
        {
            var p1 = points[i];
            var p2 = points[i + 1];
            
            if (!p1.Time.HasValue || !p2.Time.HasValue)
                continue;
                
            var distance = CalculateHaversineDistance(p1.Latitude, p1.Longitude, p2.Latitude, p2.Longitude);
            var timeDiff = (p2.Time.Value - p1.Time.Value).TotalSeconds;
            
            if (timeDiff <= 0)
                continue; // Skip invalid time intervals
                
            var speed = distance / timeDiff; // m/s
            
            // Filter out unreasonable speeds (e.g., GPS errors)
            const double maxReasonableSpeed = 100; // m/s (~360 km/h)
            
            if (speed is > 0 and < maxReasonableSpeed)
            {
                speeds.Add(speed);
            }
        }
        
        if (speeds.Count > 0)
        {
            double maxSpeed = speeds.Max();
            if (maxSpeed > result.MaxSpeed)
                result.MaxSpeed = maxSpeed;
                
            // Calculate average speed from total distance and moving time
            if (result.MovingTime.TotalSeconds > 0)
            {
                result.AverageSpeed = result.TotalDistance / result.MovingTime.TotalSeconds;
            }
        }
    }

    private List<ElevationPoint> GenerateElevationProfile(GpxDocument document, int pointCount)
    {
        // Collect all points from tracks and routes
        List<Waypoint> allPoints = new();
        
        foreach (var track in document.Tracks)
        {
            foreach (var segment in track.Segments)
            {
                allPoints.AddRange(segment.TrackPoints.Where(p => p.Elevation.HasValue));
            }
        }
        
        foreach (var route in document.Routes)
        {
            allPoints.AddRange(route.RoutePoints.Where(p => p.Elevation.HasValue));
        }
        
        if (allPoints.Count < 2)
            return new List<ElevationPoint>();
        
        
        // Calculate cumulative distances
        List<(Waypoint Point, double Distance)> pointsWithDistance = [];
        double cumulativeDistance = 0;
        
        pointsWithDistance.Add((allPoints[0], 0));
        
        for (int i = 1; i < allPoints.Count; i++)
        {
            var prev = allPoints[i - 1];
            var curr = allPoints[i];
            
            cumulativeDistance += CalculateHaversineDistance(
                prev.Latitude, prev.Longitude,
                curr.Latitude, curr.Longitude);
                
            pointsWithDistance.Add((curr, cumulativeDistance));
        }
        
        // Create simplified profile by sampling at regular distance intervals
        double totalDistance = cumulativeDistance;
        double interval = totalDistance / (pointCount - 1);
        List<ElevationPoint> profile = new();
        
        for (int i = 0; i < pointCount; i++)
        {
            double targetDistance = i * interval;
            
            // Find closest point
            var closest = pointsWithDistance.MinBy(p => Math.Abs(p.Distance - targetDistance));
            
            // Calculate grade (slope)
            double grade = 0;
            if (i > 0 && i < pointCount - 1)
            {
                var prevIndex = pointsWithDistance.FindIndex(p => p.Point == closest.Point) - 1;
                var nextIndex = pointsWithDistance.FindIndex(p => p.Point == closest.Point) + 1;
                
                if (prevIndex >= 0 && nextIndex < pointsWithDistance.Count)
                {
                    var prev = pointsWithDistance[prevIndex];
                    var next = pointsWithDistance[nextIndex];
                    
                    double elevDiff = (next.Point.Elevation ?? 0) - (prev.Point.Elevation ?? 0);
                    double distDiff = next.Distance - prev.Distance;
                    
                    if (distDiff > 0)
                    {
                        grade = (elevDiff / distDiff) * 100; // As percentage
                    }
                }
            }
            
            profile.Add(new ElevationPoint
            {
                Distance = targetDistance,
                Elevation = closest.Point.Elevation ?? 0,
                Grade = grade
            });
        }
        
        return profile;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}