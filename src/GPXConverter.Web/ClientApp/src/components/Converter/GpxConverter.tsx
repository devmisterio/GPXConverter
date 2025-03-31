import { useState } from 'react';
import { FileFormat } from '../../types/types';
import { convertGpx } from '../../services/api';

const GpxConverter = () => {
  const [file, setFile] = useState<File | null>(null);
  const [targetFormat, setTargetFormat] = useState<FileFormat>(FileFormat.Csv);
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

  const handleFormatChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setTargetFormat(e.target.value as FileFormat);
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
      const result = await convertGpx(file, targetFormat);
      
      // Create download link
      const url = window.URL.createObjectURL(result);
      const extension = targetFormat.toLowerCase();
      
      const a = document.createElement('a');
      a.href = url;
      a.download = `converted.${extension}`;
      document.body.appendChild(a);
      a.click();
      
      // Cleanup
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err) {
      console.error('Error converting file:', err);
      setError('Error converting file. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="converter-container">
      <div className="card-header">
        <h2 className="card-title">Convert GPX File</h2>
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
              id="converter-file-input"
              className="hidden-input"
            />
            <label htmlFor="converter-file-input" className="file-input-label">
              <span className="file-input-icon">📁</span>
              {file ? file.name : 'Choose a file'}
            </label>
          </div>
        </div>
        
        <div className="form-group">
          <label>Target Format:</label>
          <select value={targetFormat} onChange={handleFormatChange}>
            <option value={FileFormat.Csv}>CSV</option>
            <option value={FileFormat.Kml}>KML</option>
            <option value={FileFormat.GeoJson}>GeoJSON</option>
          </select>
        </div>
        
        {error && <div className="error-message">{error}</div>}
        
        <button 
          type="submit" 
          disabled={!file || isLoading}
          className="btn btn-primary"
        >
          {isLoading ? 'Converting...' : 'Convert'}
        </button>
      </form>
    </div>
  );
};

export default GpxConverter;