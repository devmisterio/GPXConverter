using System.Globalization;
using System.Xml;
using GPXConverter.Core.Interfaces;
using GPXConverter.Core.Models;
using Microsoft.Extensions.Logging;

namespace GPXConverter.Infrastructure.Repositories;

/// <summary>
/// Implementation of the IGpxReader interface for reading GPX files
/// </summary>
public class GpxReader(ILogger<GpxReader> logger) : IGpxReader
{
    private readonly ILogger<GpxReader> _logger = logger;

    /// <inheritdoc />
    public async Task<GpxDocument> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reading Gpx file {FilePath}", filePath);
        
        var document = new GpxDocument();

        try
        {
            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = XmlReader.Create(fileStream, new XmlReaderSettings { Async = true });

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "metadata":
                            document.Metadata = await ReadMetadataAsync(reader, cancellationToken).ConfigureAwait(false);
                            break;
                        case "wpt":
                            document.Waypoints.Add(await ReadWaypointAsync(reader, cancellationToken)); 
                            break;
                        case "rte":
                            document.Routes.Add(await ReadRouteAsync(reader, cancellationToken));
                            break;
                        case "trk":
                            document.Tracks.Add(await ReadTrackAsync(reader, cancellationToken));
                            break;
                    }
                }
            }
            
            _logger.LogInformation(
                "GPX file read successfully : {WaypointCount} waypoint, {RouteCount} route, {TrackCount} track",
                document.Waypoints.Count, document.Routes.Count, document.Tracks.Count);
            
            return document;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError("Error reading GPX file: {filePath}", filePath);
            throw;
        }
    }

    private async Task<GpxMetadata> ReadMetadataAsync(XmlReader reader, CancellationToken cancellationToken = default)
    {
        var metadata = new GpxMetadata();

        if (reader.IsEmptyElement)
            return metadata;

        var depth = reader.Depth;

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader is { NodeType: XmlNodeType.EndElement, Name: "metadata" } && reader.Depth == depth)
                break;

            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "name":
                        metadata.Name = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        break;
                    case "desc":
                        metadata.Description = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        break;
                    case "author":
                        metadata.Author = await ReadAuthorAsync(reader, cancellationToken).ConfigureAwait(false);
                        break;
                    case "time":
                        var timeStr = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        if (DateTime.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var time))
                            metadata.Time = time;
                        break;
                    case "keywords":
                        var keywords = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(keywords))
                        {
                            metadata.Keywords.AddRange(keywords.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(k => k.Trim()));
                        }
                        break;
                }
            }
        }
        
        return metadata;
    }

    private async Task<string> ReadAuthorAsync(XmlReader reader, CancellationToken cancellationToken = default)
    {
        if (!reader.IsStartElement() || reader.IsEmptyElement)
            return await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
        
        string authorName = string.Empty;
        int depth = reader.Depth;

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader is { NodeType: XmlNodeType.EndElement, Name: "author" } && reader.Depth == depth)
                break;
            
            if (reader is { NodeType: XmlNodeType.Element, Name: "name" })
            {
                authorName = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
        }
        
        return authorName;
    }

    private async Task<Waypoint> ReadWaypointAsync(XmlReader reader, CancellationToken cancellationToken = default)
    {
        var waypoint = new Waypoint();
        
        string? latStr = reader.GetAttribute("lat");
        string? lonStr = reader.GetAttribute("lon");

        if (double.TryParse(latStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
            waypoint.Latitude = lat;
        
        if (double.TryParse(lonStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
            waypoint.Longitude = lon;

        if (reader.IsEmptyElement)
            return waypoint;
        
        int depth = reader.Depth;

        waypoint.Extensions = new Dictionary<string, string>();

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader is { NodeType: XmlNodeType.EndElement, Name: "wpt" or "rtept" or "trkpt" } && reader.Depth == depth)
                break;

            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "ele":
                        var eleStr = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        if (double.TryParse(eleStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double ele))
                            waypoint.Elevation = ele;
                        break;
                    case "time":
                        var timeStr = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        if (DateTime.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var time))
                            waypoint.Time = time;
                        break;
                    case "name":
                        waypoint.Name = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        break;
                    case "desc":
                        waypoint.Description = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        break;
                    case "sym":
                        waypoint.Symbol = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        break;
                    case "extensions":
                        await ReadExtensionsAsync(reader, waypoint.Extensions, cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        if (reader.Name != "extensions" && !reader.IsEmptyElement)
                        {
                            var elementName = reader.Name;
                            var content = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                            waypoint.Extensions[elementName] = content;
                        }
                        else if (reader.IsEmptyElement)
                        {
                            await reader.ReadAsync().ConfigureAwait(false);
                        }
                        break;
                }
            }
        }
        
        return waypoint;
    }

    private async Task ReadExtensionsAsync(XmlReader reader, Dictionary<string, string> extensions,
        CancellationToken cancellationToken = default)
    {
        if (reader.IsEmptyElement)
            return;
        
        int depth = reader.Depth;

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader is { NodeType: XmlNodeType.EndElement, Name: "extensions" } && reader.Depth == depth)
                break;

            if (reader.NodeType != XmlNodeType.Element || reader.IsEmptyElement) continue;
            
            var elementName = reader.Name;
            var content = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            extensions[elementName] = content;
        }
    }

    private async Task<Route> ReadRouteAsync(XmlReader reader, CancellationToken cancellationToken = default)
    {
        Route route = new Route();
        
        if (reader.IsEmptyElement)
            return route;
            
        int depth = reader.Depth;

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (reader is { NodeType: XmlNodeType.EndElement, Name: "rte" } && reader.Depth == depth)
                break;
            
            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "name":
                        route.Name = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        break;
                    case "desc":
                        route.Description = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        break;
                    case "rtept":
                        route.RoutePoints.Add(await ReadWaypointAsync(reader, cancellationToken).ConfigureAwait(false));
                        break;
                }
            }
        }
        
        return route;
    }
    
    private async Task<Track> ReadTrackAsync(XmlReader reader, CancellationToken cancellationToken)
    {
        Track track = new Track();
        
        if (reader.IsEmptyElement)
            return track;
            
        int depth = reader.Depth;
        
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (reader is { NodeType: XmlNodeType.EndElement, Name: "trk" } && reader.Depth == depth)
                break;
                
            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "name":
                        track.Name = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        break;
                    case "desc":
                        track.Description = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        break;
                    case "trkseg":
                        track.Segments.Add(await ReadTrackSegmentAsync(reader, cancellationToken).ConfigureAwait(false));
                        break;
                }
            }
        }
        
        return track;
    }
    
    private async Task<TrackSegment> ReadTrackSegmentAsync(XmlReader reader, CancellationToken cancellationToken)
    {
        TrackSegment segment = new TrackSegment();
        
        if (reader.IsEmptyElement)
            return segment;
            
        int depth = reader.Depth;
        
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (reader is { NodeType: XmlNodeType.EndElement, Name: "trkseg" } && reader.Depth == depth)
                break;
                
            if (reader is { NodeType: XmlNodeType.Element, Name: "trkpt" })
            {
                segment.TrackPoints.Add(await ReadWaypointAsync(reader, cancellationToken).ConfigureAwait(false));
            }
        }
        
        return segment;
    }
}