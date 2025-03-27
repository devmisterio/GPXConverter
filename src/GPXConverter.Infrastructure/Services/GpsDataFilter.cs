using GPXConverter.Core.Interfaces;
using GPXConverter.Core.Models;
using Microsoft.Extensions.Logging;

namespace GPXConverter.Infrastructure.Services;

/// <summary>
/// Service for filtering and simplifying GPS data
/// </summary>
public class GpsDataFilter : IGpsDataFilter
{
    private readonly IGpxReader _gpxReader;
    private readonly IGpxWriter _gpxWriter;
    private readonly ILogger<GpsDataFilter> _logger;
    private const double EarthRadiusMeters = 6371000.0; // Earth radius in meters

    /// <summary>
    /// Initializes a new instance of the GpsDataFilter class
    /// </summary>
    /// <param name="gpxReader">GPX reader for loading files</param>
    /// <param name="gpxWriter">GPX writer for saving files</param>
    /// <param name="logger">Logger instance</param>
    public GpsDataFilter(IGpxReader gpxReader, IGpxWriter gpxWriter, ILogger<GpsDataFilter> logger)
    {
        _gpxReader = gpxReader;
        _gpxWriter = gpxWriter;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<GpxDocument> FilterByTimeRangeAsync(GpxDocument document, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Filtering GPX document by time range: {StartTime} to {EndTime}", startTime, endTime);
        
        try
        {
            // Create a deep copy of the document
            var filteredDocument = CreateDeepCopy(document);
            
            // Filter waypoints
            filteredDocument.Waypoints = FilterWaypointsByTime(document.Waypoints, startTime, endTime);
            
            // Filter routes
            foreach (var route in filteredDocument.Routes)
            {
                route.RoutePoints = FilterWaypointsByTime(route.RoutePoints, startTime, endTime);
            }
            // Remove empty routes
            filteredDocument.Routes.RemoveAll(r => r.RoutePoints.Count == 0);
            
            // Filter tracks
            foreach (var track in filteredDocument.Tracks)
            {
                foreach (var segment in track.Segments)
                {
                    segment.TrackPoints = FilterWaypointsByTime(segment.TrackPoints, startTime, endTime);
                }
                // Remove empty segments
                track.Segments.RemoveAll(s => s.TrackPoints.Count == 0);
            }
            // Remove empty tracks
            filteredDocument.Tracks.RemoveAll(t => t.Segments.Count == 0);
            
            _logger.LogInformation("Time range filtering completed: Kept {WaypointCount} waypoints, {RouteCount} routes, {TrackCount} tracks",
                filteredDocument.Waypoints.Count, filteredDocument.Routes.Count, filteredDocument.Tracks.Count);
            
            return Task.FromResult(filteredDocument);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error filtering GPX document by time range");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<GpxDocument> FilterBySpeedRangeAsync(GpxDocument document, double minSpeed, double maxSpeed, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Filtering GPX document by speed range: {MinSpeed} m/s to {MaxSpeed} m/s", minSpeed, maxSpeed);
        
        try
        {
            // Create a deep copy of the document
            var filteredDocument = CreateDeepCopy(document);
            
            // Speed filtering can only be applied to routes and tracks with time data
            // For routes
            foreach (var route in filteredDocument.Routes)
            {
                route.RoutePoints = FilterPointsBySpeed(route.RoutePoints, minSpeed, maxSpeed);
            }
            // Remove empty routes
            filteredDocument.Routes.RemoveAll(r => r.RoutePoints.Count == 0);
            
            // For tracks
            foreach (var track in filteredDocument.Tracks)
            {
                foreach (var segment in track.Segments)
                {
                    segment.TrackPoints = FilterPointsBySpeed(segment.TrackPoints, minSpeed, maxSpeed);
                }
                // Remove empty segments
                track.Segments.RemoveAll(s => s.TrackPoints.Count == 0);
            }
            // Remove empty tracks
            filteredDocument.Tracks.RemoveAll(t => t.Segments.Count == 0);
            
            _logger.LogInformation("Speed range filtering completed: Kept {RouteCount} routes, {TrackCount} tracks",
                filteredDocument.Routes.Count, filteredDocument.Tracks.Count);
            
            return Task.FromResult(filteredDocument);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error filtering GPX document by speed range");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<GpxDocument> RemoveOutliersAsync(GpxDocument document, double speedThreshold = 35.0, double elevationThreshold = 100.0, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing outliers from GPX document with speed threshold: {SpeedThreshold} m/s and elevation threshold: {ElevationThreshold} m",
            speedThreshold, elevationThreshold);
        
        try
        {
            // Create a deep copy of the document
            var filteredDocument = CreateDeepCopy(document);
            
            // For routes
            foreach (var route in filteredDocument.Routes)
            {
                route.RoutePoints = RemoveOutlierPoints(route.RoutePoints, speedThreshold, elevationThreshold);
            }
            // Remove empty routes
            filteredDocument.Routes.RemoveAll(r => r.RoutePoints.Count == 0);
            
            // For tracks
            foreach (var track in filteredDocument.Tracks)
            {
                foreach (var segment in track.Segments)
                {
                    segment.TrackPoints = RemoveOutlierPoints(segment.TrackPoints, speedThreshold, elevationThreshold);
                }
                // Remove empty segments
                track.Segments.RemoveAll(s => s.TrackPoints.Count == 0);
            }
            // Remove empty tracks
            filteredDocument.Tracks.RemoveAll(t => t.Segments.Count == 0);
            
            _logger.LogInformation("Outlier removal completed: Kept {RouteCount} routes, {TrackCount} tracks",
                filteredDocument.Routes.Count, filteredDocument.Tracks.Count);
            
            return Task.FromResult(filteredDocument);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error removing outliers from GPX document");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<GpxDocument> SimplifyAsync(GpxDocument document, double tolerance = 10.0, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Simplifying GPX document with tolerance: {Tolerance} meters", tolerance);
        
        try
        {
            // Create a deep copy of the document
            var simplifiedDocument = CreateDeepCopy(document);
            
            // Waypoints are typically not simplified as they are user-defined points of interest
            
            // For routes
            foreach (var route in simplifiedDocument.Routes)
            {
                if (route.RoutePoints.Count > 2) // Only simplify if there are more than 2 points
                {
                    route.RoutePoints = SimplifyPoints(route.RoutePoints, tolerance);
                }
            }
            
            // For tracks
            foreach (var track in simplifiedDocument.Tracks)
            {
                foreach (var segment in track.Segments)
                {
                    if (segment.TrackPoints.Count > 2) // Only simplify if there are more than 2 points
                    {
                        segment.TrackPoints = SimplifyPoints(segment.TrackPoints, tolerance);
                    }
                }
            }
            
            _logger.LogInformation("Simplification completed");
            
            return Task.FromResult(simplifiedDocument);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error simplifying GPX document");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task FilterFileByTimeRangeAsync(string inputPath, string outputPath, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Filtering GPX file by time range: {InputPath} -> {OutputPath}", inputPath, outputPath);
        
        try
        {
            var document = await _gpxReader.ReadAsync(inputPath, cancellationToken);
            
            var filteredDocument = await FilterByTimeRangeAsync(document, startTime, endTime, cancellationToken);
            
            await _gpxWriter.WriteAsync(filteredDocument, outputPath, cancellationToken);
            
            _logger.LogInformation("GPX file filtered by time range and saved to: {OutputPath}", outputPath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error filtering GPX file by time range: {InputPath}", inputPath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task FilterFileBySpeedRangeAsync(string inputPath, string outputPath, double minSpeed, double maxSpeed, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Filtering GPX file by speed range: {InputPath} -> {OutputPath}", inputPath, outputPath);
        
        try
        {
            var document = await _gpxReader.ReadAsync(inputPath, cancellationToken);
            
            var filteredDocument = await FilterBySpeedRangeAsync(document, minSpeed, maxSpeed, cancellationToken);
            
            await _gpxWriter.WriteAsync(filteredDocument, outputPath, cancellationToken);
            
            _logger.LogInformation("GPX file filtered by speed range and saved to: {OutputPath}", outputPath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error filtering GPX file by speed range: {InputPath}", inputPath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RemoveOutliersFromFileAsync(string inputPath, string outputPath, double speedThreshold = 35.0, double elevationThreshold = 100.0, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing outliers from GPX file: {InputPath} -> {OutputPath}", inputPath, outputPath);
        
        try
        {
            var document = await _gpxReader.ReadAsync(inputPath, cancellationToken);
            
            var filteredDocument = await RemoveOutliersAsync(document, speedThreshold, elevationThreshold, cancellationToken);
            
            await _gpxWriter.WriteAsync(filteredDocument, outputPath, cancellationToken);
            
            _logger.LogInformation("Outliers removed from GPX file and saved to: {OutputPath}", outputPath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error removing outliers from GPX file: {InputPath}", inputPath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SimplifyFileAsync(string inputPath, string outputPath, double tolerance = 10.0, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Simplifying GPX file: {InputPath} -> {OutputPath}", inputPath, outputPath);
        
        try
        {
            var document = await _gpxReader.ReadAsync(inputPath, cancellationToken);
            
            var simplifiedDocument = await SimplifyAsync(document, tolerance, cancellationToken);
            
            await _gpxWriter.WriteAsync(simplifiedDocument, outputPath, cancellationToken);
            
            _logger.LogInformation("GPX file simplified and saved to: {OutputPath}", outputPath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error simplifying GPX file: {InputPath}", inputPath);
            throw;
        }
    }

    #region Helper Methods

    private GpxDocument CreateDeepCopy(GpxDocument source)
    {
        var copy = new GpxDocument();
        
        // Copy metadata
        if (source.Metadata != null)
        {
            copy.Metadata = new GpxMetadata
            {
                Name = source.Metadata.Name,
                Description = source.Metadata.Description,
                Author = source.Metadata.Author,
                Time = source.Metadata.Time,
                Keywords = new List<string>(source.Metadata.Keywords)
            };
        }
        
        // Copy waypoints
        foreach (var waypoint in source.Waypoints)
        {
            copy.Waypoints.Add(CopyWaypoint(waypoint));
        }
        
        // Copy routes
        foreach (var route in source.Routes)
        {
            var routeCopy = new Route
            {
                Name = route.Name,
                Description = route.Description
            };
            
            foreach (var routePoint in route.RoutePoints)
            {
                routeCopy.RoutePoints.Add(CopyWaypoint(routePoint));
            }
            
            copy.Routes.Add(routeCopy);
        }
        
        // Copy tracks
        foreach (var track in source.Tracks)
        {
            var trackCopy = new Track
            {
                Name = track.Name,
                Description = track.Description
            };
            
            foreach (var segment in track.Segments)
            {
                var segmentCopy = new TrackSegment();
                
                foreach (var trackPoint in segment.TrackPoints)
                {
                    segmentCopy.TrackPoints.Add(CopyWaypoint(trackPoint));
                }
                
                trackCopy.Segments.Add(segmentCopy);
            }
            
            copy.Tracks.Add(trackCopy);
        }
        
        return copy;
    }

    private Waypoint CopyWaypoint(Waypoint source)
    {
        var copy = new Waypoint
        {
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            Elevation = source.Elevation,
            Time = source.Time,
            Name = source.Name,
            Description = source.Description,
            Symbol = source.Symbol
        };
        
        if (source.Extensions != null)
        {
            copy.Extensions = new Dictionary<string, string>(source.Extensions);
        }
        
        return copy;
    }

    private List<Waypoint> FilterWaypointsByTime(List<Waypoint> points, DateTime startTime, DateTime endTime)
    {
        return points.Where(p => p.Time.HasValue && p.Time.Value >= startTime && p.Time.Value <= endTime).ToList();
    }

    private List<Waypoint> FilterPointsBySpeed(List<Waypoint> points, double minSpeed, double maxSpeed)
    {
        if (points.Count < 2)
            return new List<Waypoint>(points);
            
        var result = new List<Waypoint>();
        
        // Always include the first point
        if (points.Count > 0)
            result.Add(points[0]);
            
        for (int i = 1; i < points.Count; i++)
        {
            var p1 = points[i - 1];
            var p2 = points[i];
            
            if (!p1.Time.HasValue || !p2.Time.HasValue)
            {
                // If time data is missing, keep the point
                result.Add(p2);
                continue;
            }
            
            var distance = CalculateHaversineDistance(p1.Latitude, p1.Longitude, p2.Latitude, p2.Longitude);
            var timeDiff = (p2.Time.Value - p1.Time.Value).TotalSeconds;
            
            if (timeDiff <= 0)
            {
                // Invalid time interval, keep the point
                result.Add(p2);
                continue;
            }
            
            var speed = distance / timeDiff; // m/s
            
            if (speed >= minSpeed && speed <= maxSpeed)
            {
                result.Add(p2);
            }
        }
        
        return result;
    }

    private List<Waypoint> RemoveOutlierPoints(List<Waypoint> points, double speedThreshold, double elevationThreshold)
    {
        if (points.Count < 3)
            return new List<Waypoint>(points);
            
        var result = new List<Waypoint>();
        
        // Always include the first point
        result.Add(points[0]);
        
        for (int i = 1; i < points.Count - 1; i++)
        {
            var prev = points[i - 1];
            var curr = points[i];
            var next = points[i + 1];
            
            bool isOutlier = false;
            
            // Check speed-based outliers if time data is available
            if (prev.Time.HasValue && curr.Time.HasValue)
            {
                var distance = CalculateHaversineDistance(prev.Latitude, prev.Longitude, curr.Latitude, curr.Longitude);
                var timeDiff = (curr.Time.Value - prev.Time.Value).TotalSeconds;
                
                if (timeDiff > 0)
                {
                    var speed = distance / timeDiff; // m/s
                    
                    if (speed > speedThreshold)
                    {
                        isOutlier = true;
                    }
                }
            }
            
            // Check elevation-based outliers if elevation data is available
            if (!isOutlier && prev.Elevation.HasValue && curr.Elevation.HasValue && next.Elevation.HasValue)
            {
                var elevDiff1 = Math.Abs(curr.Elevation.Value - prev.Elevation.Value);
                var elevDiff2 = Math.Abs(next.Elevation.Value - curr.Elevation.Value);
                
                // Check if the elevation difference is suspiciously large
                if (elevDiff1 > elevationThreshold && elevDiff2 > elevationThreshold)
                {
                    isOutlier = true;
                }
            }
            
            // If not an outlier, add to result
            if (!isOutlier)
            {
                result.Add(curr);
            }
        }
        
        // Always include the last point
        if (points.Count > 1)
            result.Add(points[points.Count - 1]);
            
        return result;
    }

    private List<Waypoint> SimplifyPoints(List<Waypoint> points, double toleranceMeters)
    {
        if (points.Count <= 2)
            return new List<Waypoint>(points);
            
        // Convert to array for faster indexing
        var pointsArray = points.ToArray();
        
        // Initialize the mask to mark points to keep
        var keepMask = new bool[pointsArray.Length];
        
        // Always keep first and last point
        keepMask[0] = true;
        keepMask[keepMask.Length - 1] = true;
        
        // Run the Ramer-Douglas-Peucker algorithm
        SimplifyRDP(pointsArray, 0, pointsArray.Length - 1, toleranceMeters, keepMask);
        
        // Create a new list with only the kept points
        var result = new List<Waypoint>();
        for (int i = 0; i < pointsArray.Length; i++)
        {
            if (keepMask[i])
            {
                result.Add(pointsArray[i]);
            }
        }
        
        return result;
    }

    private void SimplifyRDP(Waypoint[] points, int startIndex, int endIndex, double toleranceMeters, bool[] keepMask)
    {
        if (endIndex <= startIndex + 1)
            return;
            
        // Find the point with the maximum distance from the line segment
        double maxDistance = 0;
        int farthestIndex = startIndex;
        
        var startPoint = points[startIndex];
        var endPoint = points[endIndex];
        
        for (int i = startIndex + 1; i < endIndex; i++)
        {
            var distance = PerpendicularDistance(points[i], startPoint, endPoint);
            
            if (distance > maxDistance)
            {
                maxDistance = distance;
                farthestIndex = i;
            }
        }
        
        // If the maximum distance is greater than the tolerance, recursively simplify
        if (maxDistance > toleranceMeters)
        {
            keepMask[farthestIndex] = true;
            
            // Recursively simplify the two segments
            SimplifyRDP(points, startIndex, farthestIndex, toleranceMeters, keepMask);
            SimplifyRDP(points, farthestIndex, endIndex, toleranceMeters, keepMask);
        }
    }

    private double PerpendicularDistance(Waypoint point, Waypoint lineStart, Waypoint lineEnd)
    {
        // Calculate line length squared
        double dx = lineEnd.Longitude - lineStart.Longitude;
        double dy = lineEnd.Latitude - lineStart.Latitude;
        double lineLengthSquared = dx * dx + dy * dy;
        
        if (lineLengthSquared == 0)
            return CalculateHaversineDistance(point.Latitude, point.Longitude, lineStart.Latitude, lineStart.Longitude);
            
        // Calculate the projection factor
        double projectionFactor = ((point.Longitude - lineStart.Longitude) * dx + 
                                  (point.Latitude - lineStart.Latitude) * dy) / 
                                  lineLengthSquared;
                                  
        // Restrict to line segment
        if (projectionFactor < 0)
            projectionFactor = 0;
        else if (projectionFactor > 1)
            projectionFactor = 1;
            
        // Calculate the closest point on the line
        double closestX = lineStart.Longitude + projectionFactor * dx;
        double closestY = lineStart.Latitude + projectionFactor * dy;
        
        // Calculate the distance to the closest point
        return CalculateHaversineDistance(point.Latitude, point.Longitude, closestY, closestX);
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

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    #endregion
}