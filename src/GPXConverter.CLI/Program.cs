using System.CommandLine;
using GPXConverter.Application;
using GPXConverter.Core.Enums;
using GPXConverter.Core.Interfaces;
using GPXConverter.Core.Models;
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
            ConfigureAnalyzeCommand(host.Services),
            ConfigureFilterCommand(host.Services),
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
    
    private static Command ConfigureAnalyzeCommand(IServiceProvider services)
    {
        var command = new Command("analyze", "Analyze GPS data to calculate statistics");
        
        // Add arguments and options
        var inputOption = new Option<FileInfo>(
            new[] { "--input", "-i" },
            "Input GPX file path"
        )
        {
            IsRequired = true
        };
        
        command.AddOption(inputOption);
        
        // Set the handler
        command.SetHandler(async (FileInfo input) =>
        {
            try
            {
                await AnalyzeFile(services, input);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, inputOption);
        
        return command;
    }
    
    private static Command ConfigureFilterCommand(IServiceProvider services)
    {
        var command = new Command("filter", "Filter and clean GPS data");
        
        // Time range filter
        var timeCommand = new Command("time", "Filter GPS data by time range");
        
        var timeInputOption = new Option<FileInfo>(
            new[] { "--input", "-i" },
            "Input GPX file path"
        )
        {
            IsRequired = true
        };
        
        var timeOutputOption = new Option<FileInfo?>(
            new[] { "--output", "-o" },
            "Output GPX file path (if not specified, will be derived from input file)"
        );
        
        var startTimeOption = new Option<DateTime>(
            new[] { "--start", "-s" },
            "Start time (format: yyyy-MM-ddTHH:mm:ss)"
        )
        {
            IsRequired = true
        };
        
        var endTimeOption = new Option<DateTime>(
            new[] { "--end", "-e" },
            "End time (format: yyyy-MM-ddTHH:mm:ss)"
        )
        {
            IsRequired = true
        };
        
        timeCommand.AddOption(timeInputOption);
        timeCommand.AddOption(timeOutputOption);
        timeCommand.AddOption(startTimeOption);
        timeCommand.AddOption(endTimeOption);
        
        timeCommand.SetHandler(async (FileInfo input, FileInfo? output, DateTime startTime, DateTime endTime) =>
        {
            try
            {
                await FilterFileByTime(services, input, output, startTime, endTime);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, timeInputOption, timeOutputOption, startTimeOption, endTimeOption);
        
        // Speed range filter
        var speedCommand = new Command("speed", "Filter GPS data by speed range");
        
        var speedInputOption = new Option<FileInfo>(
            new[] { "--input", "-i" },
            "Input GPX file path"
        )
        {
            IsRequired = true
        };
        
        var speedOutputOption = new Option<FileInfo?>(
            new[] { "--output", "-o" },
            "Output GPX file path (if not specified, will be derived from input file)"
        );
        
        var minSpeedOption = new Option<double>(
            new[] { "--min", "-m" },
            "Minimum speed in m/s"
        )
        {
            IsRequired = true
        };
        
        var maxSpeedOption = new Option<double>(
            new[] { "--max", "-M" },
            "Maximum speed in m/s"
        )
        {
            IsRequired = true
        };
        
        speedCommand.AddOption(speedInputOption);
        speedCommand.AddOption(speedOutputOption);
        speedCommand.AddOption(minSpeedOption);
        speedCommand.AddOption(maxSpeedOption);
        
        speedCommand.SetHandler(async (FileInfo input, FileInfo? output, double minSpeed, double maxSpeed) =>
        {
            try
            {
                await FilterFileBySpeed(services, input, output, minSpeed, maxSpeed);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, speedInputOption, speedOutputOption, minSpeedOption, maxSpeedOption);
        
        // Outlier removal filter
        var outliersCommand = new Command("outliers", "Remove outlier points from GPS data");
        
        var outliersInputOption = new Option<FileInfo>(
            new[] { "--input", "-i" },
            "Input GPX file path"
        )
        {
            IsRequired = true
        };
        
        var outliersOutputOption = new Option<FileInfo?>(
            new[] { "--output", "-o" },
            "Output GPX file path (if not specified, will be derived from input file)"
        );
        
        var speedThresholdOption = new Option<double>(
            new[] { "--speed-threshold", "-s" },
            () => 35.0,
            "Maximum speed threshold for outlier detection in m/s"
        );
        
        var elevationThresholdOption = new Option<double>(
            new[] { "--elevation-threshold", "-e" },
            () => 100.0,
            "Maximum elevation change threshold in meters"
        );
        
        outliersCommand.AddOption(outliersInputOption);
        outliersCommand.AddOption(outliersOutputOption);
        outliersCommand.AddOption(speedThresholdOption);
        outliersCommand.AddOption(elevationThresholdOption);
        
        outliersCommand.SetHandler(async (FileInfo input, FileInfo? output, double speedThreshold, double elevationThreshold) =>
        {
            try
            {
                await RemoveOutliersFromFile(services, input, output, speedThreshold, elevationThreshold);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, outliersInputOption, outliersOutputOption, speedThresholdOption, elevationThresholdOption);
        
        // Simplify filter
        var simplifyCommand = new Command("simplify", "Simplify GPS data by reducing the number of points");
        
        var simplifyInputOption = new Option<FileInfo>(
            new[] { "--input", "-i" },
            "Input GPX file path"
        )
        {
            IsRequired = true
        };
        
        var simplifyOutputOption = new Option<FileInfo?>(
            new[] { "--output", "-o" },
            "Output GPX file path (if not specified, will be derived from input file)"
        );
        
        var toleranceOption = new Option<double>(
            new[] { "--tolerance", "-t" },
            () => 10.0,
            "Tolerance in meters for the Ramer-Douglas-Peucker algorithm"
        );
        
        simplifyCommand.AddOption(simplifyInputOption);
        simplifyCommand.AddOption(simplifyOutputOption);
        simplifyCommand.AddOption(toleranceOption);
        
        simplifyCommand.SetHandler(async (FileInfo input, FileInfo? output, double tolerance) =>
        {
            try
            {
                await SimplifyFile(services, input, output, tolerance);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, simplifyInputOption, simplifyOutputOption, toleranceOption);
        
        // Add subcommands to the filter command
        command.AddCommand(timeCommand);
        command.AddCommand(speedCommand);
        command.AddCommand(outliersCommand);
        command.AddCommand(simplifyCommand);
        
        return command;
    }
    
    private static async Task AnalyzeFile(IServiceProvider services, FileInfo input)
    {
        // Validate input file
        if (!input.Exists)
        {
            throw new FileNotFoundException($"Input file not found: {input.FullName}");
        }
        
        var analyzer = services.GetRequiredService<IGpsAnalyzer>();
        
        // Show analysis progress
        GpsAnalysisResult result = await AnsiConsole.Status()
            .StartAsync($"Analyzing {input.Name}...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.Status("Reading GPX file...");
                
                // Perform the analysis
                var analysisResult = await analyzer.AnalyzeFileAsync(input.FullName);
                
                ctx.Status("Analysis completed");
                
                return analysisResult;
            });
        
        // Display the results in a table
        AnsiConsole.MarkupLine($"[green]Analysis completed for[/] [bold]{input.Name}[/]");
        
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("Metric").Centered());
        table.AddColumn(new TableColumn("Value").Centered());
        
        table.AddRow("Total Distance", $"{result.TotalDistance / 1000:F2} km");
        table.AddRow("Total Time", FormatTimeSpan(result.TotalTime));
        table.AddRow("Moving Time", FormatTimeSpan(result.MovingTime));
        table.AddRow("Average Speed", $"{result.AverageSpeed * 3.6:F2} km/h");
        table.AddRow("Max Speed", $"{result.MaxSpeed * 3.6:F2} km/h");
        table.AddRow("Elevation Gain", $"{result.TotalAscent:F0} m");
        table.AddRow("Elevation Loss", $"{result.TotalDescent:F0} m");
        table.AddRow("Min Elevation", $"{result.MinElevation:F0} m");
        table.AddRow("Max Elevation", $"{result.MaxElevation:F0} m");
        table.AddRow("Average Grade", $"{result.AverageGrade:F1} %");
        
        AnsiConsole.Write(table);
        
        // Display elevation profile if available
        if (result.ElevationProfile != null && result.ElevationProfile.Count > 0)
        {
            AnsiConsole.MarkupLine("\n[bold]Elevation Profile:[/]");
            
            // Create a simple text-based elevation profile
            var chart = new BarChart()
                .Width(60)
                .Label("Elevation Profile")
                .CenterLabel();
            
            var maxValue = result.ElevationProfile.Max(p => p.Elevation);
            var minValue = result.ElevationProfile.Min(p => p.Elevation);
            var range = maxValue - minValue;
            
            // Sample the profile to keep the chart readable
            var sampledProfile = SampleProfile(result.ElevationProfile, 10);
            
            foreach (var point in sampledProfile)
            {
                var distanceKm = point.Distance / 1000.0;
                var elevationLabel = $"{distanceKm:F1} km";
                chart.AddItem(elevationLabel, point.Elevation - minValue, Color.Blue);
            }
            
            AnsiConsole.Write(chart);
            
            AnsiConsole.MarkupLine($"Elevation range: [blue]{minValue:F0}m[/] to [blue]{maxValue:F0}m[/]");
        }
    }
    
    private static List<ElevationPoint> SampleProfile(List<ElevationPoint> profile, int sampleCount)
    {
        if (profile.Count <= sampleCount)
            return profile;
            
        var result = new List<ElevationPoint>();
        
        // Always include first and last points
        result.Add(profile.First());
        
        // Sample points in between
        double step = (double)(profile.Count - 2) / (sampleCount - 2);
        
        for (int i = 1; i < sampleCount - 1; i++)
        {
            int index = (int)Math.Round(i * step);
            result.Add(profile[index]);
        }
        
        result.Add(profile.Last());
        
        return result;
    }
    
    private static async Task FilterFileByTime(
        IServiceProvider services,
        FileInfo input,
        FileInfo? output,
        DateTime startTime,
        DateTime endTime)
    {
        // Validate input file
        if (!input.Exists)
        {
            throw new FileNotFoundException($"Input file not found: {input.FullName}");
        }
        
        // Determine output path if not specified
        string outputPath = DetermineOutputPath(input, output, "filtered_time");
        
        var filter = services.GetRequiredService<IGpsDataFilter>();
        
        // Show filtering progress
        await AnsiConsole.Status()
            .StartAsync($"Filtering {input.Name} by time range...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.Status("Filtering GPX file...");
                
                // Perform the filtering
                await filter.FilterFileByTimeRangeAsync(input.FullName, outputPath, startTime, endTime);
                
                ctx.Status("Filtering completed");
            });
        
        // Success message
        AnsiConsole.MarkupLine($"[green]Success![/] Filtered {input.Name} by time range and saved to [bold]{outputPath}[/]");
    }
    
    private static async Task FilterFileBySpeed(
        IServiceProvider services,
        FileInfo input,
        FileInfo? output,
        double minSpeed,
        double maxSpeed)
    {
        // Validate input file
        if (!input.Exists)
        {
            throw new FileNotFoundException($"Input file not found: {input.FullName}");
        }
        
        // Determine output path if not specified
        string outputPath = DetermineOutputPath(input, output, "filtered_speed");
        
        var filter = services.GetRequiredService<IGpsDataFilter>();
        
        // Show filtering progress
        await AnsiConsole.Status()
            .StartAsync($"Filtering {input.Name} by speed range...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.Status("Filtering GPX file...");
                
                // Perform the filtering
                await filter.FilterFileBySpeedRangeAsync(input.FullName, outputPath, minSpeed, maxSpeed);
                
                ctx.Status("Filtering completed");
            });
        
        // Success message
        AnsiConsole.MarkupLine($"[green]Success![/] Filtered {input.Name} by speed range and saved to [bold]{outputPath}[/]");
    }
    
    private static async Task RemoveOutliersFromFile(
        IServiceProvider services,
        FileInfo input,
        FileInfo? output,
        double speedThreshold,
        double elevationThreshold)
    {
        // Validate input file
        if (!input.Exists)
        {
            throw new FileNotFoundException($"Input file not found: {input.FullName}");
        }
        
        // Determine output path if not specified
        string outputPath = DetermineOutputPath(input, output, "no_outliers");
        
        var filter = services.GetRequiredService<IGpsDataFilter>();
        
        // Show filtering progress
        await AnsiConsole.Status()
            .StartAsync($"Removing outliers from {input.Name}...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.Status("Processing GPX file...");
                
                // Perform the filtering
                await filter.RemoveOutliersFromFileAsync(
                    input.FullName, 
                    outputPath, 
                    speedThreshold, 
                    elevationThreshold);
                
                ctx.Status("Processing completed");
            });
        
        // Success message
        AnsiConsole.MarkupLine($"[green]Success![/] Removed outliers from {input.Name} and saved to [bold]{outputPath}[/]");
    }
    
    private static async Task SimplifyFile(
        IServiceProvider services,
        FileInfo input,
        FileInfo? output,
        double tolerance)
    {
        // Validate input file
        if (!input.Exists)
        {
            throw new FileNotFoundException($"Input file not found: {input.FullName}");
        }
        
        // Determine output path if not specified
        string outputPath = DetermineOutputPath(input, output, "simplified");
        
        var filter = services.GetRequiredService<IGpsDataFilter>();
        
        // Show simplification progress
        await AnsiConsole.Status()
            .StartAsync($"Simplifying {input.Name}...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.Status("Processing GPX file...");
                
                // Perform the simplification
                await filter.SimplifyFileAsync(input.FullName, outputPath, tolerance);
                
                ctx.Status("Processing completed");
            });
        
        // Success message
        AnsiConsole.MarkupLine($"[green]Success![/] Simplified {input.Name} and saved to [bold]{outputPath}[/]");
    }
    
    private static string DetermineOutputPath(FileInfo input, FileInfo? output, string suffix)
    {
        if (output != null)
        {
            return output.FullName;
        }
        
        string directory = input.DirectoryName ?? "";
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(input.Name);
        string extension = input.Extension;
        
        return Path.Combine(directory, $"{fileNameWithoutExt}_{suffix}{extension}");
    }
    
    private static string FormatTimeSpan(TimeSpan time)
    {
        if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours}h {time.Minutes}m {time.Seconds}s";
        }
        
        return $"{time.Minutes}m {time.Seconds}s";
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
