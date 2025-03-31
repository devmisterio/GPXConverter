import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { ElevationPoint } from '../../types/types';

interface ElevationChartProps {
  elevationData: ElevationPoint[];
}

const ElevationChart: React.FC<ElevationChartProps> = ({ elevationData }) => {
  if (!elevationData || elevationData.length === 0) {
    return <div>No elevation data available</div>;
  }

  // Format data for Recharts
  const chartData = elevationData.map(point => ({
    distance: (point.distance / 1000).toFixed(2), // Convert to km
    elevation: point.elevation
  }));

  // Find elevation range for appropriate Y axis
  const minElevation = Math.min(...elevationData.map(p => p.elevation));
  const maxElevation = Math.max(...elevationData.map(p => p.elevation));
  const elevationRange = maxElevation - minElevation;
  
  // Set Y axis domain with a 10% buffer
  const yMin = Math.floor(minElevation - elevationRange * 0.1);
  const yMax = Math.ceil(maxElevation + elevationRange * 0.1);

  return (
    <div style={{ width: '100%', height: '100%' }}>
      <ResponsiveContainer width="100%" height="100%">
        <LineChart
          data={chartData}
          margin={{
            top: 20,
            right: 30,
            left: 20,
            bottom: 20,
          }}
        >
          <CartesianGrid strokeDasharray="3 3" stroke="#e0e0e0" />
          <XAxis 
            dataKey="distance" 
            label={{ value: 'Distance (km)', position: 'insideBottomRight', offset: -10 }}
            stroke="#70757a"
            tick={{ fill: '#70757a' }}
          />
          <YAxis 
            domain={[yMin, yMax]}
            label={{ value: 'Elevation (m)', angle: -90, position: 'insideLeft' }}
            stroke="#70757a"
            tick={{ fill: '#70757a' }}
          />
          <Tooltip 
            formatter={(value: number) => [`${value.toFixed(1)} m`, 'Elevation']}
            labelFormatter={(label) => `Distance: ${label} km`}
            contentStyle={{
              backgroundColor: 'white',
              border: '1px solid #dadce0',
              borderRadius: '8px',
              padding: '10px',
              boxShadow: '0 2px 10px rgba(0,0,0,0.1)'
            }}
          />
          <Legend wrapperStyle={{ paddingTop: '10px' }} />
          <Line 
            type="monotone" 
            dataKey="elevation" 
            stroke="#4285f4" 
            strokeWidth={2}
            activeDot={{ r: 6, stroke: '#4285f4', strokeWidth: 2, fill: 'white' }} 
            dot={{ r: 0 }}
            isAnimationActive={false}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
};

export default ElevationChart;