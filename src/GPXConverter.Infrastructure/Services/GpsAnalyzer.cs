using GPXConverter.Core.Interfaces;
using GPXConverter.Core.Models;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace GPXConverter.Infrastructure.Services;

/// <summary>
/// Service for analyzing GPS data and calculating statistics
/// </summary>
public class GpsAnalyzer : IGpsAnalyzer
{
    private readonly IGpxReader _gpxReader;
    private readonly ILogger<GpsAnalyzer> _logger;
    private const double EarthRadiusMeters = 6371000.0; // Earth radius in meters
    
    // WGS84 EPSG:4326 coordinate reference system (standard GPS)
    private static readonly int WGS84_SRID = 4326;

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
        if (points.Count < 2)
            return 0;
            
        double totalDistance = 0;
        
        // Create a linestring from all points
        var coordinates = points.Select(p => new Coordinate(p.Longitude, p.Latitude)).ToArray();
        var lineString = new LineString(coordinates) { SRID = WGS84_SRID };
        
        // Calculate distance between each pair of points
        for (int i = 0; i < points.Count - 1; i++)
        {
            var p1 = points[i];
            var p2 = points[i + 1];
            
            totalDistance += CalculateGeodesicDistance(
                p1.Latitude, p1.Longitude, 
                p2.Latitude, p2.Longitude);
        }
        
        return totalDistance;
    }

    private double CalculateGeodesicDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Create points using NTS
        var point1 = new Point(lon1, lat1) { SRID = WGS84_SRID };
        var point2 = new Point(lon2, lat2) { SRID = WGS84_SRID };
        
        // Use Vincenty formula (more accurate than Haversine)
        return VincentyDistance(lat1, lon1, lat2, lon2);
    }
    
    private double VincentyDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Convert degrees to radians
        lat1 = ToRadians(lat1);
        lon1 = ToRadians(lon1);
        lat2 = ToRadians(lat2);
        lon2 = ToRadians(lon2);
        
        // WGS-84 ellipsoid parameters
        double a = 6378137.0; // semi-major axis in meters
        double f = 1/298.257223563; // flattening
        double b = a * (1 - f); // semi-minor axis
        
        double L = lon2 - lon1; // difference in longitude
        double U1 = Math.Atan((1 - f) * Math.Tan(lat1)); // reduced latitude
        double U2 = Math.Atan((1 - f) * Math.Tan(lat2)); // reduced latitude
        double sinU1 = Math.Sin(U1);
        double cosU1 = Math.Cos(U1);
        double sinU2 = Math.Sin(U2);
        double cosU2 = Math.Cos(U2);
        
        double lambda = L;
        double lambdaP;
        double sinLambda, cosLambda, sinSigma, cosSigma, sigma, sinAlpha, cosSqAlpha, cos2SigmaM;
        int iterations = 0;
        
        do {
            sinLambda = Math.Sin(lambda);
            cosLambda = Math.Cos(lambda);
            sinSigma = Math.Sqrt(Math.Pow(cosU2 * sinLambda, 2) + 
                                 Math.Pow(cosU1 * sinU2 - sinU1 * cosU2 * cosLambda, 2));
            
            if (sinSigma == 0) // coincident points
                return 0;
                
            cosSigma = sinU1 * sinU2 + cosU1 * cosU2 * cosLambda;
            sigma = Math.Atan2(sinSigma, cosSigma);
            sinAlpha = cosU1 * cosU2 * sinLambda / sinSigma;
            cosSqAlpha = 1 - sinAlpha * sinAlpha;
            
            cos2SigmaM = cosSigma - 2 * sinU1 * sinU2 / cosSqAlpha;
            if (double.IsNaN(cos2SigmaM)) // equatorial line
                cos2SigmaM = 0;
                
            double C = f / 16 * cosSqAlpha * (4 + f * (4 - 3 * cosSqAlpha));
            lambdaP = lambda;
            lambda = L + (1 - C) * f * sinAlpha * 
                    (sigma + C * sinSigma * (cos2SigmaM + C * cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM)));
                    
        } while (Math.Abs(lambda - lambdaP) > 1e-12 && ++iterations < 100);
        
        if (iterations >= 100) {
            _logger.LogWarning("Vincenty formula failed to converge, falling back to Haversine");
            return FallbackHaversineDistance(lat1, lon1, lat2, lon2);
        }
        
        double uSq = cosSqAlpha * (a * a - b * b) / (b * b);
        double A = 1 + uSq / 16384 * (4096 + uSq * (-768 + uSq * (320 - 175 * uSq)));
        double B = uSq / 1024 * (256 + uSq * (-128 + uSq * (74 - 47 * uSq)));
        double deltaSigma = B * sinSigma * (cos2SigmaM + B / 4 * (cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM) - 
                           B / 6 * cos2SigmaM * (-3 + 4 * sinSigma * sinSigma) * (-3 + 4 * cos2SigmaM * cos2SigmaM)));
                           
        // Return distance in meters
        return b * A * (sigma - deltaSigma);
    }
    
    // Fallback to use if Vincenty fails to converge
    private double FallbackHaversineDistance(double lat1Rad, double lon1Rad, double lat2Rad, double lon2Rad)
    {
        // Convert back to degrees if necessary (for function reuse)
        if (lat1Rad > Math.PI/2) {
            lat1Rad = ToRadians(lat1Rad);
            lon1Rad = ToRadians(lon1Rad);
            lat2Rad = ToRadians(lat2Rad);
            lon2Rad = ToRadians(lon2Rad);
        }
        
        // Haversine formula
        double dLat = lat2Rad - lat1Rad;
        double dLon = lon2Rad - lon1Rad;
        
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * 
                   Math.Cos(lat1Rad) * Math.Cos(lat2Rad);
        
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
                
            var distance = CalculateGeodesicDistance(p1.Latitude, p1.Longitude, p2.Latitude, p2.Longitude);
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
        if (points.Count < 2)
            return;
            
        // Apply elevation smoothing using a moving average to reduce GPS noise
        var smoothedElevations = SmoothElevationData(points);
        
        double ascent = 0;
        double descent = 0;
        double minElevation = double.MaxValue;
        double maxElevation = double.MinValue;
        double maxGrade = 0;
        double totalWeightedGrade = 0;
        double totalDistance = 0;
        
        for (int i = 0; i < points.Count; i++)
        {
            // Use smoothed elevation data
            var elevation = smoothedElevations[i];
            
            // Update min/max elevations
            if (elevation < minElevation) minElevation = elevation;
            if (elevation > maxElevation) maxElevation = elevation;
            
            // Calculate ascent/descent and grade between points
            if (i > 0)
            {
                var prevElevation = smoothedElevations[i - 1];
                var diff = elevation - prevElevation;
                
                // Filter out small elevation changes that might be noise
                const double noiseThreshold = 2.0; // 2 meters (increased from 1m)
                
                if (Math.Abs(diff) > noiseThreshold)
                {
                    if (diff > 0)
                    {
                        ascent += diff;
                    }
                    else
                    {
                        descent += Math.Abs(diff);
                    }
                    
                    // Calculate grade between these two points
                    var p1 = points[i - 1];
                    var p2 = points[i];
                    
                    var segmentDistance = CalculateGeodesicDistance(
                        p1.Latitude, p1.Longitude,
                        p2.Latitude, p2.Longitude);
                        
                    if (segmentDistance > 1.0) // Avoid division by very small numbers
                    {
                        var segmentGrade = (diff / segmentDistance) * 100; // As percentage
                        
                        // Update max grade (both positive and negative grades are considered)
                        if (Math.Abs(segmentGrade) > Math.Abs(maxGrade))
                        {
                            maxGrade = segmentGrade;
                        }
                        
                        // For average grade calculation, weight by distance
                        totalWeightedGrade += segmentGrade * segmentDistance;
                        totalDistance += segmentDistance;
                    }
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
        
        // Set max grade
        if (Math.Abs(maxGrade) > Math.Abs(result.MaxGrade))
        {
            result.MaxGrade = maxGrade;
        }
        
        // Calculate weighted average grade
        if (totalDistance > 0)
        {
            double segmentAverageGrade = totalWeightedGrade / totalDistance;
            
            // If this is the first calculation or we have a larger segment than before
            if (result.AverageGrade == 0 || totalDistance > result.TotalDistance / 2)
            {
                // Weight the average grade by segment distance relative to total distance
                double weight = totalDistance / (result.TotalDistance + 0.1); // Add small value to avoid division by zero
                result.AverageGrade = (result.AverageGrade * (1 - weight)) + (segmentAverageGrade * weight);
            }
        }
    }
    
    // Apply a moving average filter to smooth elevation data
    private double[] SmoothElevationData(IList<Waypoint> points)
    {
        const int windowSize = 3; // Use a 3-point moving average
        double[] smoothed = new double[points.Count];
        
        for (int i = 0; i < points.Count; i++)
        {
            int start = Math.Max(0, i - (int)(windowSize / 2));
            int end = Math.Min(points.Count - 1, i + windowSize / 2);
            int count = end - start + 1;
            
            double sum = 0;
            for (int j = start; j <= end; j++)
            {
                sum += points[j].Elevation ?? 0;
            }
            
            smoothed[i] = sum / count;
        }
        
        return smoothed;
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
                
            var distance = CalculateGeodesicDistance(p1.Latitude, p1.Longitude, p2.Latitude, p2.Longitude);
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
        
        // Apply smoothing to the elevation data
        var smoothedElevations = SmoothElevationData(allPoints);
        
        // Calculate cumulative distances using accurate geodesic measurements
        List<(Waypoint Point, double Distance, double Elevation)> pointsWithDistance = [];
        double cumulativeDistance = 0;
        
        // Always include the first point
        pointsWithDistance.Add((allPoints[0], 0, smoothedElevations[0]));
        
        for (int i = 1; i < allPoints.Count; i++)
        {
            var prev = allPoints[i - 1];
            var curr = allPoints[i];
            
            cumulativeDistance += CalculateGeodesicDistance(
                prev.Latitude, prev.Longitude,
                curr.Latitude, curr.Longitude);
                
            pointsWithDistance.Add((curr, cumulativeDistance, smoothedElevations[i]));
        }
        
        // Create simplified profile by sampling at regular distance intervals
        double totalDistance = cumulativeDistance;
        double interval = totalDistance / (pointCount - 1);
        List<ElevationPoint> profile = new();
        
        // Create a LineString from all points for 3D interpolation
        var coordinates = allPoints.Select(p => 
            new CoordinateZ(p.Longitude, p.Latitude, p.Elevation ?? 0)
        ).ToArray();
        var lineString = new LineString(coordinates) { SRID = WGS84_SRID };
        
        // Always include the start and end points
        profile.Add(new ElevationPoint
        {
            Distance = 0,
            Elevation = pointsWithDistance[0].Elevation,
            Grade = 0
        });
        
        // Generate intermediate points
        for (int i = 1; i < pointCount - 1; i++)
        {
            double targetDistance = i * interval;
            
            // Find the two points that the target distance falls between
            int j = 0;
            while (j < pointsWithDistance.Count - 1 && pointsWithDistance[j + 1].Distance < targetDistance)
            {
                j++;
            }
            
            if (j >= pointsWithDistance.Count - 1)
                break;
                
            var p1 = pointsWithDistance[j];
            var p2 = pointsWithDistance[j + 1];
            
            // Interpolate elevation between these two points
            double fraction = 0;
            if (p2.Distance > p1.Distance) // Prevent division by zero
            {
                fraction = (targetDistance - p1.Distance) / (p2.Distance - p1.Distance);
            }
            
            double elevation = p1.Elevation + fraction * (p2.Elevation - p1.Elevation);
            
            // Calculate the grade (slope) at this point
            double prevDist = Math.Max(0, targetDistance - interval/2);
            double nextDist = Math.Min(totalDistance, targetDistance + interval/2);
            
            // Find elevation at those distances
            var prevPoint = FindPointAtDistance(pointsWithDistance, prevDist);
            var nextPoint = FindPointAtDistance(pointsWithDistance, nextDist);
            
            double grade = 0;
            double distDiff = nextPoint.Distance - prevPoint.Distance;
            if (distDiff > 0)
            {
                double elevDiff = nextPoint.Elevation - prevPoint.Elevation;
                grade = (elevDiff / distDiff) * 100; // As percentage
            }
            
            profile.Add(new ElevationPoint
            {
                Distance = targetDistance,
                Elevation = elevation,
                Grade = grade
            });
        }
        
        // Always include the end point
        profile.Add(new ElevationPoint
        {
            Distance = totalDistance,
            Elevation = pointsWithDistance[^1].Elevation,
            Grade = profile.Count > 0 ? profile[^1].Grade : 0
        });
        
        return profile;
    }
    
    private (double Distance, double Elevation) FindPointAtDistance(
        List<(Waypoint Point, double Distance, double Elevation)> points, double targetDistance)
    {
        // If target is beyond the ends, return the end points
        if (targetDistance <= 0)
            return (0, points[0].Elevation);
            
        if (targetDistance >= points[^1].Distance)
            return (points[^1].Distance, points[^1].Elevation);
            
        // Find the two points that the target distance falls between
        int i = 0;
        while (i < points.Count - 1 && points[i + 1].Distance < targetDistance)
        {
            i++;
        }
        
        var p1 = points[i];
        var p2 = points[i + 1];
        
        // Linear interpolation
        double fraction = 0;
        if (p2.Distance > p1.Distance) // Prevent division by zero
        {
            fraction = (targetDistance - p1.Distance) / (p2.Distance - p1.Distance);
        }
        
        double elevation = p1.Elevation + fraction * (p2.Elevation - p1.Elevation);
        
        return (targetDistance, elevation);
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}