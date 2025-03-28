using GPXConverter.Core.Enums;
using GPXConverter.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GPXConverter.Application;

/// <summary>
/// Provides extension methods for registering GPX Converter services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all GPX Converter services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddGpxConverterServices(this IServiceCollection services)
    {
        // Register core services from Infrastructure
        services.AddTransient<IGpxReader, Infrastructure.Repositories.GpxReader>();
        services.AddTransient<IGpxWriter, Infrastructure.Repositories.GpxWriter>();
        
        // Register GPS analysis and filtering services
        services.AddTransient<IGpsAnalyzer, Infrastructure.Services.GpsAnalyzer>();
        services.AddTransient<IGpsDataFilter, Infrastructure.Services.GpsDataFilter>();
        
        // Register format converters
        services.AddTransient<Infrastructure.Services.GpxToCsvConverter>();
        services.AddTransient<Infrastructure.Services.GpxToGeoJsonConverter>();
        services.AddTransient<Infrastructure.Services.GpxToKmlConverter>();
        
        // Register the converters as implementations of IFormatConverter
        services.AddTransient<IFormatConverter>(sp => sp.GetRequiredService<Infrastructure.Services.GpxToCsvConverter>());
        services.AddTransient<IFormatConverter>(sp => sp.GetRequiredService<Infrastructure.Services.GpxToGeoJsonConverter>());
        services.AddTransient<IFormatConverter>(sp => sp.GetRequiredService<Infrastructure.Services.GpxToKmlConverter>());
        
        // Add factory service for selecting appropriate converter
        services.AddTransient<FormatConverterFactory>();
        
        return services;
    }
}

/// <summary>
/// Factory for creating the appropriate format converter based on the target format
/// </summary>
public class FormatConverterFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    /// <summary>
    /// Initializes a new instance of the FormatConverterFactory class
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    public FormatConverterFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    /// <summary>
    /// Creates a converter for the specified format
    /// </summary>
    /// <param name="format">Target format</param>
    /// <returns>The appropriate format converter</returns>
    public IFormatConverter CreateConverter(FileFormat format)
    {
        return format switch
        {
            FileFormat.Csv => _serviceProvider.GetRequiredService<Infrastructure.Services.GpxToCsvConverter>(),
            FileFormat.GeoJson => _serviceProvider.GetRequiredService<Infrastructure.Services.GpxToGeoJsonConverter>(),
            FileFormat.Kml => _serviceProvider.GetRequiredService<Infrastructure.Services.GpxToKmlConverter>(),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };
    }
}
