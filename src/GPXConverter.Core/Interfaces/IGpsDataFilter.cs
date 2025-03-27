using GPXConverter.Core.Models;

namespace GPXConverter.Core.Interfaces;

/// <summary>
/// Interface for filtering GPS data
/// </summary>
public interface IGpsDataFilter
{
    /// <summary>
    /// Filters GPS data by time range
    /// </summary>
    /// <param name="document">The GPX document to filter</param>
    /// <param name="startTime">Start time for filtering</param>
    /// <param name="endTime">End time for filtering</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A new filtered GPX document</returns>
    Task<GpxDocument> FilterByTimeRangeAsync(GpxDocument document, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Filters GPS data by speed range
    /// </summary>
    /// <param name="document">The GPX document to filter</param>
    /// <param name="minSpeed">Minimum speed in m/s</param>
    /// <param name="maxSpeed">Maximum speed in m/s</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A new filtered GPX document</returns>
    Task<GpxDocument> FilterBySpeedRangeAsync(GpxDocument document, double minSpeed, double maxSpeed, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes outlier points from GPS data
    /// </summary>
    /// <param name="document">The GPX document to filter</param>
    /// <param name="speedThreshold">Maximum speed threshold for outlier detection in m/s</param>
    /// <param name="elevationThreshold">Maximum elevation change threshold in meters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A new filtered GPX document</returns>
    Task<GpxDocument> RemoveOutliersAsync(GpxDocument document, double speedThreshold = 35.0, double elevationThreshold = 100.0, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Simplifies GPS data by reducing the number of points using the Ramer-Douglas-Peucker algorithm
    /// </summary>
    /// <param name="document">The GPX document to simplify</param>
    /// <param name="tolerance">Tolerance in meters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A new simplified GPX document</returns>
    Task<GpxDocument> SimplifyAsync(GpxDocument document, double tolerance = 10.0, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Filters GPS data from a file by time range
    /// </summary>
    /// <param name="inputPath">Path to the input GPX file</param>
    /// <param name="outputPath">Path to the output GPX file</param>
    /// <param name="startTime">Start time for filtering</param>
    /// <param name="endTime">End time for filtering</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Asynchronous operation</returns>
    Task FilterFileByTimeRangeAsync(string inputPath, string outputPath, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Filters GPS data from a file by speed range
    /// </summary>
    /// <param name="inputPath">Path to the input GPX file</param>
    /// <param name="outputPath">Path to the output GPX file</param>
    /// <param name="minSpeed">Minimum speed in m/s</param>
    /// <param name="maxSpeed">Maximum speed in m/s</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Asynchronous operation</returns>
    Task FilterFileBySpeedRangeAsync(string inputPath, string outputPath, double minSpeed, double maxSpeed, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes outlier points from a GPX file
    /// </summary>
    /// <param name="inputPath">Path to the input GPX file</param>
    /// <param name="outputPath">Path to the output GPX file</param>
    /// <param name="speedThreshold">Maximum speed threshold for outlier detection in m/s</param>
    /// <param name="elevationThreshold">Maximum elevation change threshold in meters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Asynchronous operation</returns>
    Task RemoveOutliersFromFileAsync(string inputPath, string outputPath, double speedThreshold = 35.0, double elevationThreshold = 100.0, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Simplifies a GPX file by reducing the number of points using the Ramer-Douglas-Peucker algorithm
    /// </summary>
    /// <param name="inputPath">Path to the input GPX file</param>
    /// <param name="outputPath">Path to the output GPX file</param>
    /// <param name="tolerance">Tolerance in meters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Asynchronous operation</returns>
    Task SimplifyFileAsync(string inputPath, string outputPath, double tolerance = 10.0, CancellationToken cancellationToken = default);
}