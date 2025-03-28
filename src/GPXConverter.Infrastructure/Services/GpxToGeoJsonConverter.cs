using System.Text.Json;
using GPXConverter.Core.Enums;
using GPXConverter.Core.Interfaces;
using GPXConverter.Core.Models;
using Microsoft.Extensions.Logging;

namespace GPXConverter.Infrastructure.Services;

/// <summary>
/// Converts GPX documents to GeoJSON format
/// </summary>
/// <param name="gpxReader">GPX reader service</param>
/// <param name="logger">Logger instance</param>
public class GpxToGeoJsonConverter(IGpxReader gpxReader, ILogger<GpxToGeoJsonConverter> logger) : IFormatConverter
{
    
    /// <inheritdoc />
    public async Task ConvertAsync(GpxDocument document, string outputPath, FileFormat format,
        CancellationToken cancellationToken = default)
    {
        if (format != FileFormat.GeoJson)
        {
            throw new ArgumentException($"Unsupported format for GpxToGeoJsonConverter: {format}. Only GeoJSON format is supported.", nameof(format));
        }
        
        logger.LogInformation("Converting GPX document to GeoJSON file: {OutputPath}", outputPath);
        
        try
        {
            var geoJson = await ConvertToGeoJsonAsync(document, cancellationToken);
            
            await File.WriteAllTextAsync(outputPath, geoJson, cancellationToken);
            
            logger.LogInformation("GPX document successfully converted to GeoJSON: {OutputPath}", outputPath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error converting GPX document to GeoJSON file: {OutputPath}", outputPath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task ConvertFileAsync(string inputPath, string outputPath, FileFormat format,
        CancellationToken cancellationToken = default)
    {
        if (format != FileFormat.GeoJson)
        {
            throw new ArgumentException($"Unsupported format for GpxToGeoJsonConverter: {format}. Only GeoJSON format is supported.", nameof(format));
        }
        
        await ConvertGpxFileToGeoJsonAsync(inputPath, outputPath, cancellationToken);
    }

    public Task<string> ConvertToGeoJsonAsync(GpxDocument document, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Converting GPX document to GeoJSON format");

        return Task.Run(() =>
        {
            try
            {
                var features = new List<object>();

                foreach (Waypoint wawpoint in document.Waypoints)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var feature = CreatePointFeature(wawpoint, "waypoint", document.Metadata);
                    
                    features.Add(feature);
                }
                
                foreach (Route route in document.Routes)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (route.RoutePoints.Count >= 2)
                    {
                        var feature = CreateLineStringFeature(route.RoutePoints, "route", route.Name, route.Description, document.Metadata);
                        features.Add(feature);
                    }
                    
                    foreach (var routePoint in route.RoutePoints)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        var feature = CreatePointFeature(routePoint, "routepoint", document.Metadata, route.Name);
                        features.Add(feature);
                    }
                }
                
                foreach (Track track in document.Tracks)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    foreach (var segment in track.Segments)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        if (segment.TrackPoints.Count >= 2)
                        {
                            var feature = CreateLineStringFeature(segment.TrackPoints, "track", track.Name, track.Description, document.Metadata);
                            features.Add(feature);
                        }
                    }
                }
                
                var result = new
                {
                    type = "FeatureCollection",
                    features = features
                };
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var geoJson = JsonSerializer.Serialize(result, options);
                
                logger.LogInformation("GPX document successfully converted to GeoJSON");
                return geoJson;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error converting GPX document to GeoJSON");
                throw;
            }
        }, cancellationToken);
    }
    
    public async Task ConvertGpxFileToGeoJsonAsync(
        string inputFilePath, 
        string outputFilePath, 
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Converting GPX file {InputFile} to GeoJSON file {OutputFile}", 
            inputFilePath, outputFilePath);
        
        try
        {
            var document = await gpxReader.ReadAsync(inputFilePath, cancellationToken);
            
            var geoJsonContent = await ConvertToGeoJsonAsync(document, cancellationToken);
            
            await File.WriteAllTextAsync(outputFilePath, geoJsonContent, cancellationToken);
            
            logger.LogInformation("GPX file successfully converted to GeoJSON: {OutputFile}", outputFilePath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error converting GPX file {InputFile} to GeoJSON", inputFilePath);
            throw;
        }
    }
    
    #region Private methods
    private object CreatePointFeature(Waypoint point, string pointType, GpxMetadata? metadata,
        string? parentName = null)
    {
        var geometry = new
        {
            type = "Point",
            coordinates = new double[]
            {
                point.Longitude,
                point.Latitude,
                point.Elevation ?? 0
            },
        };

        var properties = new Dictionary<string, object?>
        {
            { "type", pointType },
            { "name", point.Name },
            { "description", point.Description },
            { "symbol", point.Symbol },
            { "time", point.Time?.ToString("o") },
            { "parentName", parentName }
        };

        if (metadata != null)
        {
            properties["metadata_name"] = metadata.Name;
            properties["metadata_description"] = metadata.Description;
            properties["metadata_author"] = metadata.Author;
            properties["metadata_time"] = metadata.Time?.ToString("o");

            if (metadata.Keywords.Count > 0)
            {
                properties["metadata_keywords"] = metadata.Keywords;
            }
        }
        
        if (point.Extensions is { Count: > 0 })
        {
            properties["extensions"] = point.Extensions;
        }
        
        return new
        {
            type = "Feature",
            geometry,
            properties
        };
    }

    private object CreateLineStringFeature(List<Waypoint> points, string lineType, string? name, string? description,
        GpxMetadata? metadata)
    {
        var coordinates = points.Select(p => new double[]
        {
            p.Longitude,
            p.Latitude,
            p.Elevation ?? 0
        }).ToArray();

        var geometry = new
        {
            type = "LineString",
            coordinates
        };

        var properties = new Dictionary<string, object?>
        {
            { "type", lineType },
            { "name", name },
            { "description", description },
            { "pointCount", points.Count },
        };

        if (points.All(p => p.Time.HasValue))
        {
            var startTime = points.Min(p => p.Time!.Value);
            var endTime = points.Max(p => p.Time!.Value);
            
            properties["startTime"] = startTime.ToString("o");
            properties["endTime"] = endTime.ToString("o");
            properties["duration"] = (endTime - startTime).TotalSeconds;
        }
        
        if (metadata != null)
        {
            properties["metadata_name"] = metadata.Name;
            properties["metadata_description"] = metadata.Description;
            properties["metadata_author"] = metadata.Author;
            properties["metadata_time"] = metadata.Time?.ToString("o");
            
            if (metadata.Keywords.Count > 0)
            {
                properties["metadata_keywords"] = metadata.Keywords;
            }
        }
        
        return new
        {
            type = "Feature",
            geometry,
            properties
        };
    }
    
    #endregion
}