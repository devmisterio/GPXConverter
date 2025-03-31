# GPX Converter

GPX Converter is a multi-platform application for converting and analyzing GPX files. The application can be used in three different ways: CLI, Web API, and Web Interface.

## Overview

GPX Converter is a powerful application for working with GPS data, offering multiple interfaces:

- **Web Application**: User-friendly interface for map visualization, conversion, analysis, and filtering
- **RESTful API**: Programmatic access to all GPX processing features
- **CLI Tool**: Command-line interface for batch processing and automation

## Features

- **Convert GPS Data** between various formats:
   - GPX to CSV
   - GPX to GeoJSON
   - GPX to KML

- **Map Visualization**:
   - Visualize tracks, routes, and waypoints
   - Interactive map interface with Leaflet
   - Elevation profile visualization

- **Analyze GPS Data** to calculate:
   - Distance, time, and speed metrics
   - Elevation statistics (gain, loss, min/max)
   - Detailed elevation profiles

- **Filter GPS Data**:
   - Filter by time range
   - Filter by speed range
   - Remove outliers from GPS tracks
   - Simplify GPS tracks using the Ramer-Douglas-Peucker algorithm


## Architecture

The application is built using Clean Architecture principles with:

- **Core Layer**: Domain models, interfaces, and business logic
- **Infrastructure Layer**: Implementations of interfaces, external libraries integration
- **Application Layer**: Orchestration of use cases, format conversions
- **Web/API/CLI Layers**: User interfaces for different platforms

## Technology Stack

- **Backend**:
   - .NET 9.0
   - ASP.NET Core
   - NetTopologySuite (for geographic calculations)
   - ProjNET (for coordinate system transformations)

- **Frontend**:
   - React 19
   - TypeScript
   - Leaflet (for maps)
   - Recharts (for data visualization)
   - Vite (for build tooling)

## Getting Started

### Prerequisites

- .NET 9.0 SDK or higher
- Node.js 18+ and npm/yarn (for the web frontend)

### Building and Running

1. Clone the repository
```
git clone https://github.com/yourusername/gpx-converter.git
cd gpx-converter
```

2. Build and run the API
```
dotnet build
dotnet run --project src/GPXConverter.Api/GPXConverter.Api.csproj
```

3. Build and run the Web application
```
cd src/GPXConverter.Web
dotnet run
```

Or to run just the frontend with hot reloading during development:
```
cd src/GPXConverter.Web/ClientApp
npm install
npm run dev
```

4. Using the CLI tool
```
dotnet run --project src/GPXConverter.CLI/GPXConverter.CLI.csproj -- [command] [options]
```

### CLI Usage Examples

```bash
# Convert GPX to CSV
dotnet run --project src/GPXConverter.CLI/GPXConverter.CLI.csproj convert -i input.gpx -f Csv -o output.csv

# Analyze GPX file
dotnet run --project src/GPXConverter.CLI/GPXConverter.CLI.csproj analyze -i input.gpx

# Filter by speed range (m/s)
dotnet run --project src/GPXConverter.CLI/GPXConverter.CLI.csproj filter speed -i input.gpx -m 1.0 -M 10.0

# Remove outliers
dotnet run --project src/GPXConverter.CLI/GPXConverter.CLI.csproj filter outliers -i input.gpx

# Simplify GPS track
dotnet run --project src/GPXConverter.CLI/GPXConverter.CLI.csproj filter simplify -i input.gpx -t 10.0
```

## API Endpoints

The API is accessible at `http://localhost:5112/api` when running locally.

- `POST /api/gpx/analyze` - Analyze a GPX file and return statistics
- `POST /api/gpx/convert?targetFormat={format}` - Convert a GPX file to another format
- `POST /api/gpx/filter` - Filter GPX data based on various criteria
- `POST /api/gpx/elevation-profile/{trackIndex}` - Get elevation profile for a specific track

Swagger documentation is available at `/swagger` when running in development mode.

## Web Interface

The web interface provides an intuitive UI for working with GPX files:

- **Map View**: Visualize GPX data on an interactive map
- **Convert**: Convert GPX files to different formats
- **Analyze**: Get detailed statistics and visualizations of your GPS data
- **Filter**: Clean and optimize your GPX files

## Project Structure

```
GPXConverter/
├── src/
│   ├── GPXConverter.Core/             # Domain models and interfaces
│   ├── GPXConverter.Infrastructure/   # Implementations and external integrations
│   ├── GPXConverter.Application/      # Application services and use cases
│   ├── GPXConverter.CLI/              # Command-line interface
│   ├── GPXConverter.Api/              # RESTful API
│   └── GPXConverter.Web/              # Web application (ASP.NET + React)
│       └── ClientApp/                 # React frontend
└─
```

## Contributing

Contributions are welcome! Please feel free to submit pull requests.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License.
