using GPXConverter.Core.Enums;
using GPXConverter.Core.Models;

namespace GPXConverter.Core.Interfaces;

/// <summary>
/// Interface defining operations for converting GPX data to different formats
/// </summary>
public interface IFormatConverter
{
    /// <summary>
    /// Converts the GPX document to the specified format and writes it to the file
    /// </summary>
    /// <param name="document">GPX document to convert</param>
    /// <param name="outputPath">Path to output file</param>
    /// <param name="format">Output format</param>
    /// <param name="cancellationToken">Cancel Token</param>
    /// <returns>Asynchronous operation</returns>
    Task ConvertAsync(GpxDocument document, string outputPath, FileFormat format, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reads the specified GPX file and saves it in the specified format
    /// </summary>
    /// <param name="inputPath">Path to the input GPX file</param>
    /// <param name="outputPath">Path to output file</param>
    /// <param name="format">Output format</param>
    /// <param name="cancellationToken">Cancel token</param>
    /// <returns>Asynchronous operation</returns>
    Task ConvertFileAsync(string inputPath, string outputPath, FileFormat format, CancellationToken cancellationToken = default);
}