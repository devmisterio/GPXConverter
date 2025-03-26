using GPXConverter.Core.Models;

namespace GPXConverter.Core.Interfaces;

/// <summary>
/// Interface for reading GPX files
/// </summary>
public interface IGpxReader
{
    /// <summary>
    /// Reads the GPX file at the specified file path
    /// </summary>
    /// <param name="filePath">File path of the GPX file</param>
    /// <param name="cancellationToken">Cancel token</param>
    /// <returns>GpxDocument object containing the read GPX data</returns>
    Task<GpxDocument> ReadAsync(string filePath, CancellationToken cancellationToken = default);
}