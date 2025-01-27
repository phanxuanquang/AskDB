import React, { useState } from 'react';
import { Box, TextField, Typography, Select, MenuItem, FormControl, InputLabel, Button, Grid } from '@mui/material';

function DatabaseConnectionStep({ onNext, initialData }) {
    const [dbType, setDbType] = useState(initialData.dbType || '');
    const [connectionString, setConnectionString] = useState(initialData.connectionString || '');

    const handleSubmit = () => {
        if (!dbType || !connectionString) {
            alert('Please fill in all fields');
            return;
        }
        onNext({ dbType, connectionString });
    };

    return (
        <Box sx={{ p: 2 }}>
            <Typography variant="h6" mb={3}>Connect to your database</Typography>
            <Grid container spacing={2} sx={{ mb: 3 }}>
                <Grid item xs={4}>
                    <FormControl fullWidth>
                        <InputLabel>Database Type</InputLabel>
                        <Select value={dbType} onChange={(e) => setDbType(e.target.value)} label="Database Type">
                            <MenuItem value="mysql">MySQL</MenuItem>
                            <MenuItem value="postgresql">PostgreSQL</MenuItem>
                            <MenuItem value="sqlite">SQLite</MenuItem>
                        </Select>
                    </FormControl>
                </Grid>
                <Grid item xs={8}>
                    <TextField
                        fullWidth
                        label="Connection String"
                        value={connectionString}
                        onChange={(e) => setConnectionString(e.target.value)}
                    />
                </Grid>
            </Grid>
            <Button variant="contained" onClick={handleSubmit} fullWidth>
                Next
            </Button>
        </Box>
    );
}

export default DatabaseConnectionStep;
