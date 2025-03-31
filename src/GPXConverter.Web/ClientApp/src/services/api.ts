import axios from 'axios';
import { GpsAnalysisResult, ElevationPoint, FileFormat } from '../types/types';

const API_URL = 'http://localhost:5112/api'; // .NET API URL

const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'multipart/form-data',
  },
});

export const analyzeGpx = async (file: File): Promise<GpsAnalysisResult> => {
  const formData = new FormData();
  formData.append('file', file);

  console.log('Sending analyze request to:', `${API_URL}/gpx/analyze`);
  
  try {
    const response = await api.post<GpsAnalysisResult>('/gpx/analyze', formData);
    console.log('Analyze response raw:', response);
    console.log('Analyze response data:', response.data);
    
    // Convert TimeSpan string (if it's a string) to seconds
    let data = response.data;
    if (typeof data.totalTime === 'string') {
      // Try to parse .NET TimeSpan format "00:00:00"
      const timeParts = data.totalTime.split(':');
      if (timeParts.length === 3) {
        const hours = parseInt(timeParts[0]) || 0;
        const minutes = parseInt(timeParts[1]) || 0;
        const seconds = parseInt(timeParts[2]) || 0;
        data.totalTime = hours * 3600 + minutes * 60 + seconds;
      }
    }
    
    return data;
  } catch (error) {
    console.error('Analyze error:', error);
    throw error;
  }
};

export const convertGpx = async (file: File, targetFormat: FileFormat): Promise<Blob> => {
  const formData = new FormData();
  formData.append('file', file);

  const response = await api.post<Blob>(`/gpx/convert?targetFormat=${targetFormat}`, formData, {
    responseType: 'blob',
  });
  
  return response.data;
};

export const filterGpx = async (
  file: File, 
  minSpeed?: number, 
  maxSpeed?: number, 
  removeOutliers: boolean = false,
  simplifyTrack: boolean = false
): Promise<Blob> => {
  const formData = new FormData();
  formData.append('file', file);

  let url = '/gpx/filter?';
  
  if (minSpeed !== undefined) url += `minSpeed=${minSpeed}&`;
  if (maxSpeed !== undefined) url += `maxSpeed=${maxSpeed}&`;
  if (removeOutliers) url += 'removeOutliers=true&';
  if (simplifyTrack) url += 'simplifyTrack=true&';

  const response = await api.post<Blob>(url, formData, {
    responseType: 'blob',
  });
  
  return response.data;
};

export const getElevationProfile = async (file: File, trackIndex: number = 0): Promise<ElevationPoint[]> => {
  const formData = new FormData();
  formData.append('file', file);

  const response = await api.post<ElevationPoint[]>(`/gpx/elevation-profile/${trackIndex}`, formData);
  return response.data;
};