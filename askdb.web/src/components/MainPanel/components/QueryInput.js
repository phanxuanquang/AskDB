import React from 'react';
import { TextField, Alert, Box } from '@mui/material';

function QueryInput({ query, setQuery, loading, error }) {
  return (
    <Box>
      <TextField
        fullWidth
        multiline
        rows={3}
        variant="outlined"
        placeholder="Enter your query in natural language or SQL..."
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        disabled={loading}
        sx={{
          mb: 2,
          '& .MuiInputBase-root': {
            fontSize: { xs: '0.875rem', sm: '1rem' }
          }
        }}
      />

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}
    </Box>
  );
}

export default QueryInput;