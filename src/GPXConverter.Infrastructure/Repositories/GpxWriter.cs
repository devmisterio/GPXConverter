using System.Xml;
using GPXConverter.Core.Interfaces;
using GPXConverter.Core.Models;
using Microsoft.Extensions.Logging;

namespace GPXConverter.Infrastructure.Repositories;

/// <summary>
/// Implementation of the IGpxWriter interface for writing GPX files
/// </summary>
public class GpxWriter(ILogger<GpxWriter> logger) : IGpxWriter
{
    /// <inheritdoc />
    public async Task WriteAsync(GpxDocument document, string filePath, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Writing Gpx file to {FilePath}", filePath);
        
        try
        {
            var settings = new XmlWriterSettings
            {
                Async = true,
                Indent = true,
                IndentChars = "  ",
                NewLineChars = Environment.NewLine,
                NewLineHandling = NewLineHandling.Replace
            };
            
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await using var writer = XmlWriter.Create(fileStream, settings);
            
            await writer.WriteStartDocumentAsync();
            
            await writer.WriteStartElementAsync(null, "gpx", "http://www.topografix.com/GPX/1/1");
            await writer.WriteAttributeStringAsync(null, "version", null, "1.1");
            await writer.WriteAttributeStringAsync(null, "creator", null, "GPX Converter");
            await writer.WriteAttributeStringAsync("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
            await writer.WriteAttributeStringAsync("xsi", "schemaLocation", null, 
                "http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd");
            
            if (document.Metadata != null)
            {
                await WriteMetadataAsync(writer, document.Metadata, cancellationToken);
            }
            
            foreach (var waypoint in document.Waypoints)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await WriteWaypointAsync(writer, waypoint, "wpt", cancellationToken);
            }
            
            foreach (var route in document.Routes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await WriteRouteAsync(writer, route, cancellationToken);
            }
            
            foreach (var track in document.Tracks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await WriteTrackAsync(writer, track, cancellationToken);
            }
            
            await writer.WriteEndElementAsync();
            await writer.WriteEndDocumentAsync();
            await writer.FlushAsync();
            
            logger.LogInformation("GPX file written successfully: {FilePath}", filePath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error writing GPX file to {FilePath}", filePath);
            throw;
        }
    }
    
    private async Task WriteMetadataAsync(XmlWriter writer, GpxMetadata metadata, CancellationToken cancellationToken)
    {
        await writer.WriteStartElementAsync(null, "metadata", null);
        
        if (!string.IsNullOrWhiteSpace(metadata.Name))
        {
            await writer.WriteElementStringAsync(null, "name", null, metadata.Name);
        }
        
        if (!string.IsNullOrWhiteSpace(metadata.Description))
        {
            await writer.WriteElementStringAsync(null, "desc", null, metadata.Description);
        }
        
        if (!string.IsNullOrWhiteSpace(metadata.Author))
        {
            await writer.WriteStartElementAsync(null, "author", null);
            await writer.WriteElementStringAsync(null, "name", null, metadata.Author);
            await writer.WriteEndElementAsync();
        }
        
        if (metadata.Time.HasValue)
        {
            await writer.WriteElementStringAsync(null, "time", null, 
                metadata.Time.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }
        
        if (metadata.Keywords.Count > 0)
        {
            await writer.WriteElementStringAsync(null, "keywords", null, string.Join(", ", metadata.Keywords));
        }
        
        await writer.WriteEndElementAsync();
    }
    
    private async Task WriteWaypointAsync(XmlWriter writer, Waypoint waypoint, string elementName, CancellationToken cancellationToken)
    {
        await writer.WriteStartElementAsync(null, elementName, null);
        
        // Latitude and longitude are required attributes
        await writer.WriteAttributeStringAsync(null, "lat", null, waypoint.Latitude.ToString("0.###############"));
        await writer.WriteAttributeStringAsync(null, "lon", null, waypoint.Longitude.ToString("0.###############"));
        
        if (waypoint.Elevation.HasValue)
        {
            await writer.WriteElementStringAsync(null, "ele", null, waypoint.Elevation.Value.ToString("0.#"));
        }
        
        if (waypoint.Time.HasValue)
        {
            await writer.WriteElementStringAsync(null, "time", null, 
                waypoint.Time.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }
        
        if (!string.IsNullOrWhiteSpace(waypoint.Name))
        {
            await writer.WriteElementStringAsync(null, "name", null, waypoint.Name);
        }
        
        if (!string.IsNullOrWhiteSpace(waypoint.Description))
        {
            await writer.WriteElementStringAsync(null, "desc", null, waypoint.Description);
        }
        
        if (!string.IsNullOrWhiteSpace(waypoint.Symbol))
        {
            await writer.WriteElementStringAsync(null, "sym", null, waypoint.Symbol);
        }
        
        if (waypoint.Extensions is { Count: > 0 })
        {
            await writer.WriteStartElementAsync(null, "extensions", null);
            
            foreach (var extension in waypoint.Extensions)
            {
                await writer.WriteElementStringAsync(null, extension.Key, null, extension.Value);
            }
            
            await writer.WriteEndElementAsync();
        }
        
        await writer.WriteEndElementAsync();
    }
    
    private async Task WriteRouteAsync(XmlWriter writer, Route route, CancellationToken cancellationToken)
    {
        await writer.WriteStartElementAsync(null, "rte", null);
        
        if (!string.IsNullOrWhiteSpace(route.Name))
        {
            await writer.WriteElementStringAsync(null, "name", null, route.Name);
        }
        
        if (!string.IsNullOrWhiteSpace(route.Description))
        {
            await writer.WriteElementStringAsync(null, "desc", null, route.Description);
        }
        
        foreach (var routePoint in route.RoutePoints)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteWaypointAsync(writer, routePoint, "rtept", cancellationToken);
        }
        
        await writer.WriteEndElementAsync();
    }
    
    private async Task WriteTrackAsync(XmlWriter writer, Track track, CancellationToken cancellationToken)
    {
        await writer.WriteStartElementAsync(null, "trk", null);
        
        if (!string.IsNullOrWhiteSpace(track.Name))
        {
            await writer.WriteElementStringAsync(null, "name", null, track.Name);
        }
        
        if (!string.IsNullOrWhiteSpace(track.Description))
        {
            await writer.WriteElementStringAsync(null, "desc", null, track.Description);
        }
        
        foreach (var segment in track.Segments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteTrackSegmentAsync(writer, segment, cancellationToken);
        }
        
        await writer.WriteEndElementAsync();
    }
    
    private async Task WriteTrackSegmentAsync(XmlWriter writer, TrackSegment segment, CancellationToken cancellationToken)
    {
        await writer.WriteStartElementAsync(null, "trkseg", null);
        
        foreach (var trackPoint in segment.TrackPoints)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteWaypointAsync(writer, trackPoint, "trkpt", cancellationToken);
        }
        
        await writer.WriteEndElementAsync();
    }
}