import { useState } from 'react';
import './App.css';
import GpxMap from './components/Map/GpxMap';
import GpxConverter from './components/Converter/GpxConverter';
import GpxAnalyzer from './components/Analysis/GpxAnalyzer';
import GpxFilter from './components/Filter/GpxFilter';
import { GpxDocument } from './types/types';

function App() {
  const [activeTab, setActiveTab] = useState('map');
  const [gpxData, setGpxData] = useState<GpxDocument | undefined>(undefined);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const handleFileUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setSelectedFile(file);
    
    // Read GPX file for the map
    const reader = new FileReader();
    reader.onload = (e) => {
      const text = e.target?.result as string;
      
      // Simple parsing of GPX - in a real app would use the API
      try {
        const parser = new DOMParser();
        const xmlDoc = parser.parseFromString(text, "text/xml");
        
        // Helper function to safely parse coordinates that might use comma as decimal separator
        const parseCoordinate = (value: string | null): number => {
          if (!value) return 0;
          // Replace comma with dot if present (some GPX files use comma as decimal separator)
          const normalizedValue = value.replace(',', '.');
          return parseFloat(normalizedValue);
        };
        
        // Parse tracks
        const tracks = Array.from(xmlDoc.getElementsByTagName('trk')).map(track => {
          const name = track.getElementsByTagName('name')[0]?.textContent || undefined;
          const segments = Array.from(track.getElementsByTagName('trkseg')).map(segment => {
            const points = Array.from(segment.getElementsByTagName('trkpt')).map(point => {
              const lat = parseCoordinate(point.getAttribute('lat'));
              const lon = parseCoordinate(point.getAttribute('lon'));
              const ele = parseCoordinate(point.getElementsByTagName('ele')[0]?.textContent || '0');
              
              console.log(`Parsed track point: lat=${lat}, lon=${lon}, ele=${ele}`);
              
              return {
                latitude: lat,
                longitude: lon,
                elevation: ele,
                time: point.getElementsByTagName('time')[0]?.textContent || undefined
              };
            });
            return { points };
          });
          return { name, segments };
        });
        
        // Parse routes
        const routes = Array.from(xmlDoc.getElementsByTagName('rte')).map(route => {
          const name = route.getElementsByTagName('name')[0]?.textContent || undefined;
          const points = Array.from(route.getElementsByTagName('rtept')).map(point => {
            const lat = parseCoordinate(point.getAttribute('lat'));
            const lon = parseCoordinate(point.getAttribute('lon'));
            const ele = parseCoordinate(point.getElementsByTagName('ele')[0]?.textContent || '0');
            
            return {
              latitude: lat,
              longitude: lon,
              elevation: ele,
              time: point.getElementsByTagName('time')[0]?.textContent || undefined
            };
          });
          return { name, points };
        });
        
        // Parse waypoints
        const waypoints = Array.from(xmlDoc.getElementsByTagName('wpt')).map(point => {
          const lat = parseCoordinate(point.getAttribute('lat'));
          const lon = parseCoordinate(point.getAttribute('lon'));
          const ele = parseCoordinate(point.getElementsByTagName('ele')[0]?.textContent || '0');
          
          return {
            latitude: lat,
            longitude: lon,
            elevation: ele,
            time: point.getElementsByTagName('time')[0]?.textContent || undefined
          };
        });
        
        setGpxData({ tracks, routes, waypoints });
      } catch (error) {
        console.error('Error parsing GPX file:', error);
      }
    };
    
    reader.readAsText(file);
  };

  const renderNavItem = (id: string, label: string, icon: string) => (
    <li 
      className={`nav-item ${activeTab === id ? 'active' : ''}`}
      onClick={() => setActiveTab(id)}
    >
      <span className="nav-icon">{icon}</span>
      {label}
    </li>
  );

  return (
    <div className="app-container">
      <div className="app-sidebar">
        <div className="app-sidebar-header">
          <span className="app-logo">GPX Converter</span>
        </div>
        <nav className="app-nav">
          <ul className="nav-list">
            {renderNavItem('map', 'Map View', '🗺️')}
            {renderNavItem('convert', 'Convert', '🔄')}
            {renderNavItem('analyze', 'Analyze', '📊')}
            {renderNavItem('filter', 'Filter', '🔍')}
          </ul>
        </nav>
      </div>

      <div className="app-main">
        <div className="app-header">
          <h2>{activeTab === 'map' ? 'Map View' : 
              activeTab === 'convert' ? 'Convert GPX' :
              activeTab === 'analyze' ? 'Analyze GPX' : 'Filter GPX'}</h2>
          
          {/* Only show file upload in map view */}
          {activeTab === 'map' && (
            <div className="file-upload">
              <input 
                type="file" 
                accept=".gpx" 
                onChange={handleFileUpload} 
                id="gpx-file"
              />
              <label htmlFor="gpx-file">
                <span className="file-upload-icon">📁</span>
                {selectedFile ? selectedFile.name : 'Upload GPX File'}
              </label>
            </div>
          )}
        </div>

        <div className="app-content">
          {activeTab === 'map' && !gpxData && (
            <div className="no-data">
              <div className="no-data-icon">📍</div>
              <p>Upload a GPX file to get started</p>
            </div>
          )}
          
          {(activeTab !== 'map' || gpxData) && (
            <>
              {activeTab === 'map' && <GpxMap gpxData={gpxData} />}
              {activeTab === 'convert' && <GpxConverter />}
              {activeTab === 'analyze' && <GpxAnalyzer />}
              {activeTab === 'filter' && <GpxFilter />}
            </>
          )}
        </div>
      </div>
    </div>
  );
}

export default App;