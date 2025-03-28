# GPX Converter

A comprehensive .NET application for converting, analyzing, and filtering GPS data files.

## Features

- **Convert GPS data** between various formats:
  - GPX to CSV
  - GPX to GeoJSON
  - GPX to KML

- **Analyze GPS data** to calculate:
  - Distance, time, and speed metrics
  - Elevation statistics (gain, loss, min/max)
  - Detailed elevation profiles

- **Filter GPS data**:
  - By time range
  - By speed range
  - Remove outliers from GPS tracks
  - Simplify GPS tracks using the Ramer-Douglas-Peucker algorithm

## Architecture

GPX Converter is built following Clean Architecture principles with:

- **Core Layer**: Contains domain models, interfaces, and enums
- **Application Layer**: Contains business logic and services
- **Infrastructure Layer**: Implements data access and external services
- **CLI Layer**: Command-line interface for user interaction

## Getting Started

### Prerequisites

- .NET 9.0 or higher

### Installation

1. Clone the repository
2. Build the solution:
   ```
   dotnet build
   ```
3. Run the CLI application:
   ```
   dotnet run --project src/GPXConverter.CLI/GPXConverter.CLI.csproj
   ```

## Usage

### Convert GPX to Other Formats

```bash
# Convert GPX to CSV
dotnet run --project src/GPXConverter.CLI/GPXConverter.CLI.csproj convert -i input.gpx -f Csv

# Convert GPX to GeoJSON
dotnet run --project src/GPXConverter.CLI/GPXConverter.CLI.csproj convert -i input.gpx -f GeoJson

# Convert GPX to KML
dotnet run --project src/GPXConverter.CLI/GPXConverter.CLI.csproj convert -i input.gpx -f Kml

# Specify custom output file
dotnet run --project src/GPXConverter.CLI/GPXConverter.CLI.csproj convert -i input.gpx -f Csv -o output.csv
```

### Analyze GPX Data

```bash
# Analyze GPX data to calculate statistics
dotnet run --project src/GPXConverter.CLI/GPXConverter.CLI.csproj analyze -i input.gpx
```

### Filter GPX Data

```bash
# Filter by time range
dotnet run --project src/GPXConverter.CLI/GPXConverter.CLI.csproj filter time -i input.gpx -s "2023-01-01T12:00:00" -e "2023-01-01T13:00:00"

# Filter by speed range (m/s)
dotnet run --project src/GPXConverter.CLI/GPXConverter.CLI.csproj filter speed -i input.gpx -m 1.0 -M 10.0

# Remove outliers
dotnet run --project src/GPXConverter.CLI/GPXConverter.CLI.csproj filter outliers -i input.gpx -s 35.0 -e 100.0

# Simplify GPS track
dotnet run --project src/GPXConverter.CLI/GPXConverter.CLI.csproj filter simplify -i input.gpx -t 10.0
```

## License

This project is licensed under the MIT License.