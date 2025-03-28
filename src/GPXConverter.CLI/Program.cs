using System.CommandLine;
using GPXConverter.Application;
using GPXConverter.Core.Enums;
using GPXConverter.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace GPXConverter.CLI;

public class Program
{
    static async Task<int> Main(string[] args)
    {
        // Create a host builder with DI services
        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register all services using the extension method
                services.AddGpxConverterServices();
                services.AddLogging(configure => configure.AddConsole());
            })
            .Build();
        
        // Create the root command
        var rootCommand = new RootCommand("GPS Data Converter - Convert, analyze, and filter GPS data files")
        {
            ConfigureConvertCommand(host.Services),
        };
        
        return await rootCommand.InvokeAsync(args);
    }
    
    private static Command ConfigureConvertCommand(IServiceProvider services)
    {
        var command = new Command("convert", "Convert GPS data files between different formats");
        
        // Add arguments and options
        var inputOption = new Option<FileInfo>(
            new[] { "--input", "-i" },
            "Input file path"
        )
        {
            IsRequired = true
        };
        
        var outputOption = new Option<FileInfo?>(
            new[] { "--output", "-o" },
            "Output file path (if not specified, will be derived from input file)"
        );
        
        var formatOption = new Option<FileFormat>(
            new[] { "--format", "-f" },
            "Output format"
        )
        {
            IsRequired = true
        };
        
        command.AddOption(inputOption);
        command.AddOption(outputOption);
        command.AddOption(formatOption);
        
        // Set the handler
        command.SetHandler(async (FileInfo input, FileInfo? output, FileFormat format) =>
        {
            try
            {
                await ConvertFile(services, input, output, format);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, inputOption, outputOption, formatOption);
        
        return command;
    }
    
    private static async Task ConvertFile(
        IServiceProvider services, 
        FileInfo input, 
        FileInfo? output, 
        FileFormat format)
    {
        // Validate input file
        if (!input.Exists)
        {
            throw new FileNotFoundException($"Input file not found: {input.FullName}");
        }
        
        // Determine output path if not specified
        string outputPath;
        if (output == null)
        {
            string extension = format switch
            {
                FileFormat.Gpx => ".gpx",
                FileFormat.Csv => ".csv",
                FileFormat.GeoJson => ".geojson",
                FileFormat.Kml => ".kml",
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };
            
            outputPath = Path.ChangeExtension(input.FullName, extension);
        }
        else
        {
            outputPath = output.FullName;
        }
        
        // Get the appropriate converter using the factory
        var factory = services.GetRequiredService<FormatConverterFactory>();
        IFormatConverter converter;
        
        try
        {
            converter = factory.CreateConverter(format);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"No converter available for format: {format}", ex);
        }
        
        // Show conversion progress
        await AnsiConsole.Status()
            .StartAsync($"Converting {input.Name} to {format} format...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.Status("Reading GPX file...");
                
                // Perform the conversion
                await converter.ConvertFileAsync(input.FullName, outputPath, format);
                
                ctx.Status("Conversion completed");
            });
        
        // Success message
        AnsiConsole.MarkupLine($"[green]Success![/] Converted {input.Name} to [bold]{outputPath}[/]");
    }
}
