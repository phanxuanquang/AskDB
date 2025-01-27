import React, { useState } from 'react';
import { Button, TextField, MenuItem } from '@mui/material';

const ConnectDatabase = ({ onNext }) => {
  const [databaseType, setDatabaseType] = useState('');
  const [connectionString, setConnectionString] = useState('');

  const handleDatabaseTypeChange = (event) => {
    setDatabaseType(event.target.value);
  };

  const handleConnectionStringChange = (event) => {
    setConnectionString(event.target.value);
  };

  const handleNext = () => {
    // Add validation or processing logic here if needed
    onNext();
  };

  return (
    <div>
      <h2>Connect to Database</h2>
      <TextField
        select
        label="Database Type"
        value={databaseType}
        onChange={handleDatabaseTypeChange}
        fullWidth
        margin="normal"
      >
        <MenuItem value="mysql">MySQL</MenuItem>
        <MenuItem value="postgresql">PostgreSQL</MenuItem>
        <MenuItem value="mongodb">MongoDB</MenuItem>
        <MenuItem value="sqlite">SQLite</MenuItem>
      </TextField>
      <TextField
        label="Connection String"
        value={connectionString}
        onChange={handleConnectionStringChange}
        fullWidth
        margin="normal"
      />
      <Button variant="contained" color="primary" onClick={handleNext}>
        Next
      </Button>
    </div>
  );
};

export default ConnectDatabase;