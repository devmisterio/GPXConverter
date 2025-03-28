using System.Globalization;
using System.Xml;
using GPXConverter.Core.Enums;
using GPXConverter.Core.Interfaces;
using GPXConverter.Core.Models;
using Microsoft.Extensions.Logging;

namespace GPXConverter.Infrastructure.Services;

/// <summary>
/// Converts GPX documents to KML format
/// </summary>
/// <param name="gpxReader">GPX reader service</param>
/// <param name="logger">Logger instance</param>
public class GpxToKmlConverter(IGpxReader gpxReader, ILogger<GpxToKmlConverter> logger) : IFormatConverter
{
    /// <inheritdoc />
    public async Task ConvertAsync(GpxDocument document, string outputPath, FileFormat format,
        CancellationToken cancellationToken = default)
    {
        if (format != FileFormat.Kml)
        {
            throw new ArgumentException($"Unsupported format for GpxToKmlConverter: {format}. Only KML format is supported.", nameof(format));
        }
        
        logger.LogInformation("Converting GPX document to KML file: {OutputPath}", outputPath);
        
        try
        {
            var kmlContent = await ConvertToKmlAsync(document, cancellationToken);
            
            await File.WriteAllTextAsync(outputPath, kmlContent, cancellationToken);
            
            logger.LogInformation("GPX document successfully converted to KML: {OutputPath}", outputPath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error converting GPX document to KML file: {OutputPath}", outputPath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task ConvertFileAsync(string inputPath, string outputPath, FileFormat format,
        CancellationToken cancellationToken = default)
    {
        if (format != FileFormat.Kml)
        {
            throw new ArgumentException($"Unsupported format for GpxToKmlConverter: {format}. Only KML format is supported.", nameof(format));
        }
        
        await ConvertGpxFileToKmlAsync(inputPath, outputPath, cancellationToken);
    }
    
    /// <summary>
    /// Converts a GPX document to KML format
    /// </summary>
    /// <param name="document">The GPX document to convert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The KML content as a string</returns>
    public Task<string> ConvertToKmlAsync(GpxDocument document, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Converting GPX document to KML format");
        
        return Task.Run(() =>
        {
            try
            {
                using var stringWriter = new StringWriter();
                using var writer = XmlWriter.Create(stringWriter, new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = "\n",
                    NewLineHandling = NewLineHandling.Replace
                });
                
                // Write KML header
                writer.WriteStartDocument();
                writer.WriteStartElement("kml", "http://www.opengis.net/kml/2.2");
                writer.WriteStartElement("Document");
                
                // Write document name and description from metadata if available
                if (document.Metadata != null)
                {
                    if (!string.IsNullOrEmpty(document.Metadata.Name))
                    {
                        writer.WriteElementString("name", document.Metadata.Name);
                    }
                    
                    if (!string.IsNullOrEmpty(document.Metadata.Description))
                    {
                        writer.WriteElementString("description", document.Metadata.Description);
                    }
                }
                
                // Create style for waypoints
                writer.WriteStartElement("Style");
                writer.WriteAttributeString("id", "waypoint-style");
                writer.WriteStartElement("IconStyle");
                writer.WriteStartElement("Icon");
                writer.WriteElementString("href", "http://maps.google.com/mapfiles/kml/pushpin/ylw-pushpin.png");
                writer.WriteEndElement(); // Icon
                writer.WriteEndElement(); // IconStyle
                writer.WriteEndElement(); // Style
                
                // Create style for routes/tracks
                writer.WriteStartElement("Style");
                writer.WriteAttributeString("id", "line-style");
                writer.WriteStartElement("LineStyle");
                writer.WriteElementString("color", "ff0000ff"); // Red
                writer.WriteElementString("width", "4");
                writer.WriteEndElement(); // LineStyle
                writer.WriteEndElement(); // Style
                
                // Process waypoints
                foreach (var waypoint in document.Waypoints)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    WriteWaypoint(writer, waypoint, "waypoint");
                }
                
                // Process routes
                foreach (var route in document.Routes)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    WriteRoute(writer, route);
                }
                
                // Process tracks
                foreach (var track in document.Tracks)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    WriteTrack(writer, track);
                }
                
                writer.WriteEndElement(); // Document
                writer.WriteEndElement(); // kml
                writer.WriteEndDocument();
                
                writer.Flush();
                
                string kmlContent = stringWriter.ToString();
                logger.LogInformation("GPX document successfully converted to KML");
                return kmlContent;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error converting GPX document to KML");
                throw;
            }
        }, cancellationToken);
    }
    
    /// <summary>
    /// Converts a GPX file to KML format and saves it to the specified file
    /// </summary>
    /// <param name="inputFilePath">Path to the input GPX file</param>
    /// <param name="outputFilePath">Path to the output KML file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ConvertGpxFileToKmlAsync(
        string inputFilePath, 
        string outputFilePath, 
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Converting GPX file {InputFile} to KML file {OutputFile}", 
            inputFilePath, outputFilePath);
        
        try
        {
            var document = await gpxReader.ReadAsync(inputFilePath, cancellationToken);
            
            var kmlContent = await ConvertToKmlAsync(document, cancellationToken);
            
            await File.WriteAllTextAsync(outputFilePath, kmlContent, cancellationToken);
            
            logger.LogInformation("GPX file successfully converted to KML: {OutputFile}", outputFilePath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error converting GPX file {InputFile} to KML", inputFilePath);
            throw;
        }
    }
    
    #region Private methods
    
    private void WriteWaypoint(XmlWriter writer, Waypoint waypoint, string type, string? parentName = null)
    {
        writer.WriteStartElement("Placemark");
        
        // Write name if available
        if (!string.IsNullOrEmpty(waypoint.Name))
        {
            writer.WriteElementString("name", waypoint.Name);
        }
        else if (!string.IsNullOrEmpty(parentName))
        {
            writer.WriteElementString("name", $"{parentName} point");
        }
        
        // Write description if available
        if (!string.IsNullOrEmpty(waypoint.Description))
        {
            writer.WriteElementString("description", waypoint.Description);
        }
        
        // Add extended data
        writer.WriteStartElement("ExtendedData");
        
        // Add type
        writer.WriteStartElement("Data");
        writer.WriteAttributeString("name", "type");
        writer.WriteElementString("value", type);
        writer.WriteEndElement(); // Data
        
        // Add time if available
        if (waypoint.Time.HasValue)
        {
            writer.WriteStartElement("Data");
            writer.WriteAttributeString("name", "time");
            writer.WriteElementString("value", waypoint.Time.Value.ToString("o"));
            writer.WriteEndElement(); // Data
        }
        
        // Add elevation if available
        if (waypoint.Elevation.HasValue)
        {
            writer.WriteStartElement("Data");
            writer.WriteAttributeString("name", "elevation");
            writer.WriteElementString("value", waypoint.Elevation.Value.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement(); // Data
        }
        
        // Add any custom extensions
        if (waypoint.Extensions != null)
        {
            foreach (var extension in waypoint.Extensions)
            {
                writer.WriteStartElement("Data");
                writer.WriteAttributeString("name", extension.Key);
                writer.WriteElementString("value", extension.Value);
                writer.WriteEndElement(); // Data
            }
        }
        
        writer.WriteEndElement(); // ExtendedData
        
        // Set style
        writer.WriteElementString("styleUrl", "#waypoint-style");
        
        // Write point geometry
        writer.WriteStartElement("Point");
        
        // Add coordinates (longitude,latitude,elevation)
        string coordinates = $"{waypoint.Longitude.ToString(CultureInfo.InvariantCulture)},{waypoint.Latitude.ToString(CultureInfo.InvariantCulture)}";
        if (waypoint.Elevation.HasValue)
        {
            coordinates += $",{waypoint.Elevation.Value.ToString(CultureInfo.InvariantCulture)}";
        }
        writer.WriteElementString("coordinates", coordinates);
        
        writer.WriteEndElement(); // Point
        writer.WriteEndElement(); // Placemark
    }
    
    private void WriteRoute(XmlWriter writer, Route route)
    {
        // Skip empty routes
        if (route.RoutePoints.Count == 0)
            return;
        
        // Write route as LineString
        writer.WriteStartElement("Placemark");
        
        // Write name if available
        if (!string.IsNullOrEmpty(route.Name))
        {
            writer.WriteElementString("name", route.Name);
        }
        else
        {
            writer.WriteElementString("name", "Route");
        }
        
        // Write description if available
        if (!string.IsNullOrEmpty(route.Description))
        {
            writer.WriteElementString("description", route.Description);
        }
        
        // Set style
        writer.WriteElementString("styleUrl", "#line-style");
        
        // Write LineString geometry
        writer.WriteStartElement("LineString");
        writer.WriteElementString("tessellate", "1");
        writer.WriteElementString("altitudeMode", "clampToGround");
        
        // Write coordinates
        var coordinatesList = route.RoutePoints.Select(p =>
        {
            string coord = $"{p.Longitude.ToString(CultureInfo.InvariantCulture)},{p.Latitude.ToString(CultureInfo.InvariantCulture)}";
            if (p.Elevation.HasValue)
            {
                coord += $",{p.Elevation.Value.ToString(CultureInfo.InvariantCulture)}";
            }
            return coord;
        });
        
        writer.WriteElementString("coordinates", string.Join(" ", coordinatesList));
        
        writer.WriteEndElement(); // LineString
        writer.WriteEndElement(); // Placemark
        
        // Also write each route point as a Placemark
        foreach (var routePoint in route.RoutePoints)
        {
            WriteWaypoint(writer, routePoint, "routepoint", route.Name);
        }
    }
    
    private void WriteTrack(XmlWriter writer, Track track)
    {
        // Skip empty tracks
        if (track.Segments.Count == 0)
            return;
        
        // Write the whole track as a MultiGeometry
        writer.WriteStartElement("Placemark");
        
        // Write name if available
        if (!string.IsNullOrEmpty(track.Name))
        {
            writer.WriteElementString("name", track.Name);
        }
        else
        {
            writer.WriteElementString("name", "Track");
        }
        
        // Write description if available
        if (!string.IsNullOrEmpty(track.Description))
        {
            writer.WriteElementString("description", track.Description);
        }
        
        // Set style
        writer.WriteElementString("styleUrl", "#line-style");
        
        if (track.Segments.Count > 1)
        {
            writer.WriteStartElement("MultiGeometry");
        }
        
        // Write each segment as a LineString
        foreach (var segment in track.Segments)
        {
            // Skip empty segments
            if (segment.TrackPoints.Count == 0)
                continue;
            
            writer.WriteStartElement("LineString");
            writer.WriteElementString("tessellate", "1");
            writer.WriteElementString("altitudeMode", "clampToGround");
            
            // Write coordinates
            var coordinatesList = segment.TrackPoints.Select(p =>
            {
                string coord = $"{p.Longitude.ToString(CultureInfo.InvariantCulture)},{p.Latitude.ToString(CultureInfo.InvariantCulture)}";
                if (p.Elevation.HasValue)
                {
                    coord += $",{p.Elevation.Value.ToString(CultureInfo.InvariantCulture)}";
                }
                return coord;
            });
            
            writer.WriteElementString("coordinates", string.Join(" ", coordinatesList));
            
            writer.WriteEndElement(); // LineString
        }
        
        if (track.Segments.Count > 1)
        {
            writer.WriteEndElement(); // MultiGeometry
        }
        
        writer.WriteEndElement(); // Placemark
        
        // Also write each track point as a Placemark
        int segmentIndex = 0;
        foreach (var segment in track.Segments)
        {
            foreach (var trackPoint in segment.TrackPoints)
            {
                var segmentName = track.Segments.Count > 1 ? $"{track.Name} (segment {segmentIndex + 1})" : track.Name;
                WriteWaypoint(writer, trackPoint, "trackpoint", segmentName);
            }
            segmentIndex++;
        }
    }
    
    #endregion
}