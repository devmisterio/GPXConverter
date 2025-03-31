import { useState } from 'react';
import { GpsAnalysisResult, ElevationPoint } from '../../types/types';
import { analyzeGpx, getElevationProfile } from '../../services/api';
import ElevationChart from './ElevationChart';

const GpxAnalyzer = () => {
  const [file, setFile] = useState<File | null>(null);
  const [analysisResult, setAnalysisResult] = useState<GpsAnalysisResult | null>(null);
  const [elevationData, setElevationData] = useState<ElevationPoint[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (selectedFile && selectedFile.name.endsWith('.gpx')) {
      setFile(selectedFile);
      setError(null);
      // Reset results when new file selected
      setAnalysisResult(null);
      setElevationData([]);
    } else {
      setFile(null);
      setError('Please select a valid GPX file');
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!file) {
      setError('Please select a file');
      return;
    }

    setIsLoading(true);
    setError(null);
    setAnalysisResult(null);
    setElevationData([]);

    try {
      console.log('Starting GPX analysis with file:', file.name);
      
      // Analyze the GPX file
      const result = await analyzeGpx(file);
      console.log('Analysis result received:', result);
      
      if (!result) {
        throw new Error('No analysis result returned from API');
      }
      
      // Ensure all numeric properties have valid values
      const sanitizedResult = {
        ...result,
        totalDistance: typeof result.totalDistance === 'number' ? result.totalDistance : 0,
        totalTime: typeof result.totalTime === 'number' ? result.totalTime : 0,
        elevationGain: typeof result.elevationGain === 'number' ? result.elevationGain : 0,
        elevationLoss: typeof result.elevationLoss === 'number' ? result.elevationLoss : 0,
        maxElevation: typeof result.maxElevation === 'number' ? result.maxElevation : 0,
        minElevation: typeof result.minElevation === 'number' ? result.minElevation : 0,
        averageSpeed: typeof result.averageSpeed === 'number' ? result.averageSpeed : 0,
        maxSpeed: typeof result.maxSpeed === 'number' ? result.maxSpeed : 0
      };
      
      console.log('Sanitized result:', sanitizedResult);
      setAnalysisResult(sanitizedResult);
      
      try {
        // Get elevation profile for visualization (wrap in separate try/catch)
        const elevationProfile = await getElevationProfile(file, 0); // Default to first track
        if (elevationProfile && elevationProfile.length > 0) {
          setElevationData(elevationProfile);
        }
      } catch (elevationErr) {
        console.error('Error getting elevation profile:', elevationErr);
        // Still continue with the analysis result even if elevation profile fails
      }
    } catch (err) {
      console.error('Error analyzing file:', err);
      setError('Error analyzing file. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  // Format duration in hours, minutes, seconds
  const formatDuration = (seconds: number | string | null | undefined): string => {
    if (seconds === null || seconds === undefined) {
      return "00:00:00";
    }
    
    if (typeof seconds === 'string') {
      // If it's already a formatted string like "00:00:00", return it
      if (/^\d{1,2}:\d{2}:\d{2}$/.test(seconds)) {
        return seconds;
      }
      
      // Otherwise try to parse it
      const parsed = parseFloat(seconds);
      if (isNaN(parsed)) {
        return "00:00:00";
      }
      seconds = parsed;
    }
    
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = Math.floor(seconds % 60);
    
    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  return (
    <div className="analyzer-container">
      <div className="card-header">
        <h2 className="card-title">Analyze GPX File</h2>
      </div>
      
      {/* Only show form when no analysis result */}
      {!analysisResult && (
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Select GPX File:</label>
            <div className="file-input-wrapper">
              <input 
                type="file" 
                accept=".gpx" 
                onChange={handleFileChange}
                required
                id="analyzer-file-input"
                className="hidden-input"
              />
              <label htmlFor="analyzer-file-input" className="file-input-label">
                <span className="file-input-icon">📁</span>
                {file ? file.name : 'Choose a file'}
              </label>
            </div>
          </div>
          
          {error && <div className="error-message">{error}</div>}
          
          <button 
            type="submit" 
            disabled={!file || isLoading}
            className="btn btn-primary"
          >
            {isLoading ? 'Analyzing...' : 'Analyze'}
          </button>
        </form>
      )}

      {isLoading && (
        <div className="loading-indicator">
          <p>Analyzing your GPX file...</p>
        </div>
      )}

      {analysisResult && (
        <div className="analysis-results-container">
          <div className="card">
            <div className="card-header">
              <h3 className="card-title">Analysis Results</h3>
            </div>
            <div className="results-grid">
              <div className="result-item">
                <div className="result-label">Total Distance</div>
                <div className="result-value">{(analysisResult.totalDistance / 1000).toFixed(2)} km</div>
              </div>
              <div className="result-item">
                <div className="result-label">Total Time</div>
                <div className="result-value">{formatDuration(analysisResult.totalTime)}</div>
              </div>
              <div className="result-item">
                <div className="result-label">Average Speed</div>
                <div className="result-value">{analysisResult.averageSpeed.toFixed(1)} km/h</div>
              </div>
              <div className="result-item">
                <div className="result-label">Max Speed</div>
                <div className="result-value">{analysisResult.maxSpeed.toFixed(1)} km/h</div>
              </div>
              <div className="result-item">
                <div className="result-label">Elevation Gain</div>
                <div className="result-value">{analysisResult.elevationGain.toFixed(0)} m</div>
              </div>
              <div className="result-item">
                <div className="result-label">Elevation Loss</div>
                <div className="result-value">{analysisResult.elevationLoss.toFixed(0)} m</div>
              </div>
              <div className="result-item">
                <div className="result-label">Max Elevation</div>
                <div className="result-value">{analysisResult.maxElevation.toFixed(0)} m</div>
              </div>
              <div className="result-item">
                <div className="result-label">Min Elevation</div>
                <div className="result-value">{analysisResult.minElevation.toFixed(0)} m</div>
              </div>
            </div>
          </div>

          {elevationData.length > 0 && (
            <div className="elevation-chart-container">
              <div className="card-header">
                <h3 className="card-title">Elevation Profile</h3>
              </div>
              <div className="elevation-chart-wrapper">
                <ElevationChart elevationData={elevationData} />
              </div>
            </div>
          )}

          <button 
            onClick={() => {
              setAnalysisResult(null);
              setElevationData([]);
              setFile(null);
            }}
            className="btn btn-secondary"
            style={{ marginTop: '20px' }}
          >
            Analyze Another File
          </button>
        </div>
      )}
    </div>
  );
};

export default GpxAnalyzer;