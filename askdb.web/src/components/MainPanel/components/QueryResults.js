import React from 'react';
import { Box, Typography } from '@mui/material';
import { useTheme } from '@mui/material';

function QueryResults({ queryResponse }) {
  const theme = useTheme();

  if (!queryResponse?.QueryResult) return null;

  return (
    <Box sx={{ 
      mt: 2,
      overflowX: 'auto',
      border: 1,
      borderColor: 'divider',
      borderRadius: 1,
      p: 2
    }}>
      <Typography variant="h6" mb={2}>Results</Typography>
      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead>
          <tr>
            {Object.keys(queryResponse.QueryResult[0]).map((header) => (
              <th key={header} style={{ 
                textAlign: 'left', 
                padding: '12px 8px',
                borderBottom: `1px solid ${theme.palette.divider}`,
                backgroundColor: theme.palette.background.default
              }}>
                {header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {queryResponse.QueryResult.map((row, i) => (
            <tr key={i}>
              {Object.values(row).map((cell, j) => (
                <td key={j} style={{ 
                  padding: '8px',
                  borderBottom: `1px solid ${theme.palette.divider}`
                }}>
                  {cell}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </Box>
  );
}

export default QueryResults;