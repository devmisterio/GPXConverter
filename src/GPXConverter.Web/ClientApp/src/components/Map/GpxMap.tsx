import { useEffect, useState } from 'react';
import { MapContainer, TileLayer, Polyline, Marker, Popup, useMap } from 'react-leaflet';
import { LatLngExpression, LatLngBounds, LatLng } from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { GpxDocument, Track, Route, Waypoint } from '../../types/types';

// Fix for Leaflet marker icons in React
import L from 'leaflet';
import icon from 'leaflet/dist/images/marker-icon.png';
import iconShadow from 'leaflet/dist/images/marker-shadow.png';

let DefaultIcon = L.icon({
  iconUrl: icon,
  shadowUrl: iconShadow,
  iconSize: [25, 41],
  iconAnchor: [12, 41]
});

L.Marker.prototype.options.icon = DefaultIcon;

// Helper component to update map view when center changes
function MapUpdater({ center, zoom, bounds }: { center?: LatLngExpression, zoom?: number, bounds?: LatLngBounds }) {
  const map = useMap();
  
  useEffect(() => {
    if (bounds && bounds.isValid()) {
      map.fitBounds(bounds, { padding: [50, 50] });
    } else if (center) {
      map.setView(center, zoom || map.getZoom());
    }
  }, [map, center, zoom, bounds]);
  
  return null;
}

interface GpxMapProps {
  gpxData?: GpxDocument;
  file?: File;
}

const GpxMap: React.FC<GpxMapProps> = ({ gpxData }) => {
  const [center, setCenter] = useState<LatLngExpression>([51.505, -0.09]);
  const [zoom, setZoom] = useState(13);
  const [bounds, setBounds] = useState<LatLngBounds | undefined>(undefined);
  
  useEffect(() => {
    if (gpxData) {
      // Collect all points
      const allPoints: Waypoint[] = [
        ...gpxData.waypoints || [],
        ...(gpxData.tracks || []).flatMap(track => 
          track.segments.flatMap(segment => segment.points)
        ),
        ...(gpxData.routes || []).flatMap(route => route.points)
      ];

      if (allPoints.length > 0) {
        // Calculate center
        const latSum = allPoints.reduce((sum, point) => sum + point.latitude, 0);
        const lngSum = allPoints.reduce((sum, point) => sum + point.longitude, 0);
        setCenter([latSum / allPoints.length, lngSum / allPoints.length]);
        
        // Calculate bounds
        if (allPoints.length > 1) {
          const latLngs = allPoints.map(point => new LatLng(point.latitude, point.longitude));
          const newBounds = new LatLngBounds(latLngs);
          setBounds(newBounds);
          
          // Adjust zoom based on bounds extent
          const boundsWidth = Math.abs(newBounds.getEast() - newBounds.getWest());
          const boundsHeight = Math.abs(newBounds.getNorth() - newBounds.getSouth());
          
          // Smaller area = higher zoom
          if (boundsWidth < 0.01 && boundsHeight < 0.01) {
            setZoom(16); // Very close zoom for small areas
          } else if (boundsWidth < 0.1 && boundsHeight < 0.1) {
            setZoom(14); // Close zoom
          } else if (boundsWidth < 1 && boundsHeight < 1) {
            setZoom(12); // Medium zoom
          } else {
            setZoom(10); // Farther zoom for large areas
          }
        } else {
          // Single point, set a closer zoom
          setZoom(15);
          setBounds(undefined);
        }
      }
    }
  }, [gpxData]);

  if (!gpxData) {
    return (
      <div className="no-data" style={{ height: '100%' }}>
        <div className="no-data-icon">📍</div>
        <p>Please upload a GPX file to view the map</p>
      </div>
    );
  }

  const renderTracks = () => {
    return (gpxData.tracks || []).map((track: Track, trackIndex: number) => {
      return track.segments.map((segment, segmentIndex: number) => {
        const points: LatLngExpression[] = segment.points.map(point => [
          point.latitude,
          point.longitude
        ]);
        
        return (
          <Polyline 
            key={`track-${trackIndex}-segment-${segmentIndex}`}
            positions={points}
            color="blue"
            weight={3}
          />
        );
      });
    });
  };

  const renderRoutes = () => {
    return (gpxData.routes || []).map((route: Route, routeIndex: number) => {
      const points: LatLngExpression[] = route.points.map(point => [
        point.latitude,
        point.longitude
      ]);
      
      return (
        <Polyline 
          key={`route-${routeIndex}`}
          positions={points}
          color="green" 
          weight={3}
          dashArray="5, 5"
        />
      );
    });
  };

  const renderWaypoints = () => {
    return (gpxData.waypoints || []).map((waypoint: Waypoint, index: number) => (
      <Marker 
        key={`waypoint-${index}`}
        position={[waypoint.latitude, waypoint.longitude]}
      >
        <Popup>
          <div>
            <h3>Waypoint {index + 1}</h3>
            {waypoint.elevation && <p>Elevation: {waypoint.elevation.toFixed(1)} m</p>}
            {waypoint.time && <p>Time: {new Date(waypoint.time).toLocaleString()}</p>}
          </div>
        </Popup>
      </Marker>
    ));
  };

  // Default initial center/zoom if no data
  const initialCenter: LatLngExpression = center || [51.505, -0.09];
  const initialZoom = zoom || 13;

  return (
    <div className="map-container" style={{ height: '100%' }}>
      <MapContainer 
        center={initialCenter}
        zoom={initialZoom}
        style={{ height: '100%', width: '100%', borderRadius: 'var(--border-radius)' }}
      >
        <MapUpdater center={center} zoom={zoom} bounds={bounds} />
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        {renderTracks()}
        {renderRoutes()}
        {renderWaypoints()}
      </MapContainer>
    </div>
  );
};

export default GpxMap;