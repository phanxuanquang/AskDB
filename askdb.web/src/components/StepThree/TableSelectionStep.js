import React, { useState } from 'react';
import { Box, TextField, Typography, List, ListItem, ListItemIcon, Checkbox, ListItemText, Button } from '@mui/material';

function TableSelectionStep({ onComplete, onSkip, initialData }) {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedTables, setSelectedTables] = useState(initialData.selectedTables || []);
  
  // Mock tables - replace with actual DB tables
  const tables = ['users', 'products', 'orders', 'categories'];

  const filteredTables = tables.filter(table => 
    table.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const handleToggle = (table) => {
    const newSelected = selectedTables.includes(table)
      ? selectedTables.filter(t => t !== table)
      : [...selectedTables, table];
    setSelectedTables(newSelected);
  };

  return (
    <Box sx={{ p: 2 }}>
      <Typography variant="h6" mb={3}>Select tables to analyze</Typography>
      <TextField
        fullWidth
        label="Search tables"
        value={searchTerm}
        onChange={(e) => setSearchTerm(e.target.value)}
        sx={{ mb: 2 }}
      />
      <List sx={{ maxHeight: 300, overflow: 'auto', mb: 3 }}>
        {filteredTables.map((table) => (
          <ListItem key={table} dense button onClick={() => handleToggle(table)}>
            <ListItemIcon>
              <Checkbox checked={selectedTables.includes(table)} />
            </ListItemIcon>
            <ListItemText primary={table} />
          </ListItem>
        ))}
      </List>
      <Box sx={{ display: 'flex', gap: 2 }}>
        <Button onClick={onSkip}>Skip</Button>
        <Button variant="contained" onClick={() => onComplete({ selectedTables })}>
          Start
        </Button>
      </Box>
    </Box>
  );
}

export default TableSelectionStep;
