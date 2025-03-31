import { useState } from 'react';
import { filterGpx } from '../../services/api';

const GpxFilter = () => {
  const [file, setFile] = useState<File | null>(null);
  const [minSpeed, setMinSpeed] = useState<string>('');
  const [maxSpeed, setMaxSpeed] = useState<string>('');
  const [removeOutliers, setRemoveOutliers] = useState(false);
  const [simplifyTrack, setSimplifyTrack] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (selectedFile && selectedFile.name.endsWith('.gpx')) {
      setFile(selectedFile);
      setError(null);
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

    try {
      // Convert string values to numbers or undefined
      const minSpeedValue = minSpeed ? parseFloat(minSpeed) : undefined;
      const maxSpeedValue = maxSpeed ? parseFloat(maxSpeed) : undefined;
      
      const result = await filterGpx(
        file, 
        minSpeedValue, 
        maxSpeedValue, 
        removeOutliers, 
        simplifyTrack
      );
      
      // Create download link
      const url = window.URL.createObjectURL(result);
      
      const a = document.createElement('a');
      a.href = url;
      a.download = 'filtered.gpx';
      document.body.appendChild(a);
      a.click();
      
      // Cleanup
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err) {
      console.error('Error filtering file:', err);
      setError('Error filtering file. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="filter-container">
      <div className="card-header">
        <h2 className="card-title">Filter GPX Data</h2>
      </div>
      
      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label>Select GPX File:</label>
          <div className="file-input-wrapper">
            <input 
              type="file" 
              accept=".gpx" 
              onChange={handleFileChange}
              required
              id="filter-file-input"
              className="hidden-input"
            />
            <label htmlFor="filter-file-input" className="file-input-label">
              <span className="file-input-icon">📁</span>
              {file ? file.name : 'Choose a file'}
            </label>
          </div>
        </div>
        
        <div className="card">
          <div className="card-header">
            <h3 className="card-title">Filter Options</h3>
          </div>
          <div className="filter-options">
            <div className="form-group">
              <label>Min Speed (km/h):</label>
              <input 
                type="number" 
                value={minSpeed}
                onChange={(e) => setMinSpeed(e.target.value)}
                min="0"
                step="0.1"
                placeholder="Optional"
              />
            </div>
            
            <div className="form-group">
              <label>Max Speed (km/h):</label>
              <input 
                type="number"
                value={maxSpeed}
                onChange={(e) => setMaxSpeed(e.target.value)}
                min="0"
                step="0.1"
                placeholder="Optional"
              />
            </div>
            
            <div className="form-group checkbox">
              <label>
                <input 
                  type="checkbox"
                  checked={removeOutliers}
                  onChange={(e) => setRemoveOutliers(e.target.checked)}
                />
                Remove Outliers
              </label>
            </div>
            
            <div className="form-group checkbox">
              <label>
                <input 
                  type="checkbox"
                  checked={simplifyTrack}
                  onChange={(e) => setSimplifyTrack(e.target.checked)}
                />
                Simplify Track
              </label>
            </div>
          </div>
        </div>
        
        {error && <div className="error-message">{error}</div>}
        
        <button 
          type="submit" 
          disabled={!file || isLoading}
          className="btn btn-primary"
        >
          <span className="btn-icon">🔍</span>
          {isLoading ? 'Filtering...' : 'Filter and Download'}
        </button>
      </form>
    </div>
  );
};

export default GpxFilter;