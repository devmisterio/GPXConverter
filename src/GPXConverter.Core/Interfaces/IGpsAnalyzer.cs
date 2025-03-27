using GPXConverter.Core.Models;

namespace GPXConverter.Core.Interfaces;

/// <summary>
/// Interface for analyzing GPS data and calculating statistics
/// </summary>
public interface IGpsAnalyzer
{
    /// <summary>
    /// Analyzes a GPX document and calculates various statistics
    /// </summary>
    /// <param name="document">The GPX document to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The analysis results</returns>
    Task<GpsAnalysisResult> AnalyzeAsync(GpxDocument document, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reads a GPX file and analyzes it, calculating various statistics
    /// </summary>
    /// <param name="filePath">Path to the GPX file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The analysis results</returns>
    Task<GpsAnalysisResult> AnalyzeFileAsync(string filePath, CancellationToken cancellationToken = default);
}