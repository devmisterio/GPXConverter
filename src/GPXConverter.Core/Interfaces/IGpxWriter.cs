using GPXConverter.Core.Models;

namespace GPXConverter.Core.Interfaces;

/// <summary>
/// Interface defining operations for writing GPX data to a file
/// </summary>
public interface IGpxWriter
{
    /// <summary>
    /// Writes the GpxDocument object to the specified file path
    /// </summary>
    /// <param name="document">GPX document to write</param>
    /// <param name="filePath">File path to write to</param>
    /// <param name="cancellationToken">Cancel token</param>
    /// <returns>Asynchronous operation</returns>
    Task WriteAsync(GpxDocument document, string filePath, CancellationToken cancellationToken = default);
}