export interface GpxDocument {
  tracks: Track[];
  routes: Route[];
  waypoints: Waypoint[];
  metadata?: GpxMetadata;
}

export interface Track {
  name?: string;
  segments: TrackSegment[];
}

export interface TrackSegment {
  points: Waypoint[];
}

export interface Route {
  name?: string;
  points: Waypoint[];
}

export interface Waypoint {
  latitude: number;
  longitude: number;
  elevation?: number;
  time?: string;
}

export interface GpxMetadata {
  name?: string;
  description?: string;
  author?: string;
  time?: string;
}

export interface GpsAnalysisResult {
  totalDistance: number;
  totalTime: number | string; // Handles both number of seconds or string format
  movingTime?: number | string;
  elevationGain: number;
  elevationLoss: number;
  maxElevation: number;
  minElevation: number;
  averageSpeed: number;
  maxSpeed: number;
  totalAscent?: number;
  totalDescent?: number;
  averageGrade?: number;
  maxGrade?: number;
  elevationProfile?: ElevationPoint[];
}

export interface ElevationPoint {
  distance: number;
  elevation: number;
}

export enum FileFormat {
  Gpx = "Gpx",
  Csv = "Csv",
  Kml = "Kml",
  GeoJson = "GeoJson"
}

export interface ApiResponse<T> {
  data: T;
  isSuccess: boolean;
  error?: string;
}