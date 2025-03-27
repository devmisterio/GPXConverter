using System.Globalization;
using System.Text;
using GPXConverter.Core.Enums;
using GPXConverter.Core.Interfaces;
using GPXConverter.Core.Models;
using Microsoft.Extensions.Logging;

namespace GPXConverter.Infrastructure.Services;

/// <summary>
/// Converts GPX documents to CSV format
/// </summary>
public class GpxToCsvConverter : IFormatConverter
{
    private readonly IGpxReader _gpxReader;
    private readonly ILogger<GpxToCsvConverter> _logger;
    
    /// <summary>
    /// Initializes a new instance of the GpxToCsvConverter
    /// </summary>
    /// <param name="gpxReader">GPX reader service</param>
    /// <param name="logger">Logger instance</param>
    public GpxToCsvConverter(IGpxReader gpxReader, ILogger<GpxToCsvConverter> logger)
    {
        _gpxReader = gpxReader;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task ConvertAsync(GpxDocument document, string outputPath, FileFormat format, CancellationToken cancellationToken = default)
    {
        if (format != FileFormat.Csv)
        {
            throw new ArgumentException($"Unsupported format for GpxToCsvConverter: {format}. Only CSV format is supported.", nameof(format));
        }
        
        _logger.LogInformation("Converting GPX document to CSV file: {OutputPath}", outputPath);
        
        try
        {
            var csvContent = await ConvertToCsvAsync(document, true, true, cancellationToken);
            
            await File.WriteAllTextAsync(outputPath, csvContent, cancellationToken);
            
            _logger.LogInformation("GPX document successfully converted to CSV: {OutputPath}", outputPath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error converting GPX document to CSV file: {OutputPath}", outputPath);
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task ConvertFileAsync(string inputPath, string outputPath, FileFormat format, CancellationToken cancellationToken = default)
    {
        if (format != FileFormat.Csv)
        {
            throw new ArgumentException($"Unsupported format for GpxToCsvConverter: {format}. Only CSV format is supported.", nameof(format));
        }
        
        await ConvertGpxFileToCsvAsync(inputPath, outputPath, true, true, cancellationToken);
    }
    
    /// <summary>
    /// Converts a GPX document to CSV format
    /// </summary>
    /// <param name="document">The GPX document to convert</param>
    /// <param name="includeHeaders">Whether to include column headers</param>
    /// <param name="includeMetadata">Whether to include metadata fields (name, description)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The CSV content as a string</returns>
    public Task<string> ConvertToCsvAsync(
        GpxDocument document, 
        bool includeHeaders = true, 
        bool includeMetadata = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Converting GPX document to CSV format");
        
        // For large documents, we can use Task.Run to perform the CPU-bound work
        // on a background thread without blocking the calling thread
        return Task.Run(() => {
            var csvBuilder = new StringBuilder();
            
            try
            {
                if (includeHeaders)
                {
                    csvBuilder.AppendLine(GenerateHeaderRow(includeMetadata));
                }
                
                foreach (var waypoint in document.Waypoints)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    csvBuilder.AppendLine(
                        FormatPointAsCsvRow(waypoint, "waypoint", document.Metadata, includeMetadata));
                }
                
                foreach (var route in document.Routes)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    foreach (var routePoint in route.RoutePoints)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        csvBuilder.AppendLine(
                            FormatPointAsCsvRow(routePoint, "route", document.Metadata, includeMetadata, route.Name));
                    }
                }
                
                foreach (var track in document.Tracks)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    foreach (var segment in track.Segments)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        foreach (var trackPoint in segment.TrackPoints)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            csvBuilder.AppendLine(
                                FormatPointAsCsvRow(trackPoint, "track", document.Metadata, includeMetadata, track.Name));
                        }
                    }
                }
                
                _logger.LogInformation("GPX document successfully converted to CSV");
                return csvBuilder.ToString();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error converting GPX document to CSV");
                throw;
            }
        }, cancellationToken);
    }
    
    /// <summary>
    /// Converts a GPX file to CSV format and saves it to the specified file
    /// </summary>
    /// <param name="inputFilePath">Path to the input GPX file</param>
    /// <param name="outputFilePath">Path to the output CSV file</param>
    /// <param name="includeHeaders">Whether to include column headers</param>
    /// <param name="includeMetadata">Whether to include metadata fields</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ConvertGpxFileToCsvAsync(
        string inputFilePath, 
        string outputFilePath, 
        bool includeHeaders = true,
        bool includeMetadata = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Converting GPX file {InputFile} to CSV file {OutputFile}", 
            inputFilePath, outputFilePath);
        
        try
        {
            var document = await _gpxReader.ReadAsync(inputFilePath, cancellationToken);
            
            var csvContent = await ConvertToCsvAsync(document, includeHeaders, includeMetadata, cancellationToken);
            
            await File.WriteAllTextAsync(outputFilePath, csvContent, cancellationToken);
            
            _logger.LogInformation("GPX file successfully converted to CSV: {OutputFile}", outputFilePath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error converting GPX file {InputFile} to CSV", inputFilePath);
            throw;
        }
    }
    
    private string GenerateHeaderRow(bool includeMetadata)
    {
        var headers = new List<string>
        {
            "Type",
            "Latitude",
            "Longitude",
            "Elevation",
            "Time",
            "Name",
            "Description",
            "Symbol"
        };

        if (!includeMetadata) return string.Join(",", headers.Select(EscapeCsvField));
        
        headers.Add("ParentName");
        headers.Add("MetadataName");
        headers.Add("MetadataDescription");
        headers.Add("MetadataAuthor");
        headers.Add("MetadataTime");

        return string.Join(",", headers.Select(EscapeCsvField));
    }
    
    private string FormatPointAsCsvRow(
        Waypoint point, 
        string pointType, 
        GpxMetadata? metadata, 
        bool includeMetadata, 
        string? parentName = null)
    {
        var culture = CultureInfo.InvariantCulture;
        var fields = new List<string>
        {
            EscapeCsvField(pointType),
            point.Latitude.ToString(culture),
            point.Longitude.ToString(culture),
            point.Elevation?.ToString(culture) ?? string.Empty,
            point.Time?.ToString("yyyy-MM-ddTHH:mm:ssZ", culture) ?? string.Empty,
            EscapeCsvField(point.Name ?? string.Empty),
            EscapeCsvField(point.Description ?? string.Empty),
            EscapeCsvField(point.Symbol ?? string.Empty)
        };

        if (!includeMetadata) return string.Join(",", fields);
        
        fields.Add(EscapeCsvField(parentName ?? string.Empty));
        fields.Add(EscapeCsvField(metadata?.Name ?? string.Empty));
        fields.Add(EscapeCsvField(metadata?.Description ?? string.Empty));
        fields.Add(EscapeCsvField(metadata?.Author ?? string.Empty));
        fields.Add(metadata?.Time?.ToString("yyyy-MM-ddTHH:mm:ssZ", culture) ?? string.Empty);

        return string.Join(",", fields);
    }
    
    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;
        
        if (field.Contains(',') || field.Contains('\"') || field.Contains('\n') || field.Contains('\r'))
            return $"\"{field.Replace("\"", "\"\"")}\"";
        
        return field;
    }
}