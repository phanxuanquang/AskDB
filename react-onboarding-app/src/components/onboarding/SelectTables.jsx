import React, { useState, useEffect } from 'react';
import { Checkbox, FormControlLabel, TextField, Button } from '@mui/material';
import { useOnboarding } from '../../hooks/useOnboarding';

const SelectTables = () => {
  const { tables, selectedTables, setSelectedTables, nextStep } = useOnboarding();
  const [searchTerm, setSearchTerm] = useState('');

  const filteredTables = tables.filter(table =>
    table.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const handleCheckboxChange = (table) => {
    setSelectedTables(prevSelected => 
      prevSelected.includes(table) 
        ? prevSelected.filter(t => t !== table) 
        : [...prevSelected, table]
    );
  };

  return (
    <div>
      <h2>Select Tables for Analysis</h2>
      <TextField
        label="Search Tables"
        variant="outlined"
        fullWidth
        value={searchTerm}
        onChange={(e) => setSearchTerm(e.target.value)}
      />
      <div>
        {filteredTables.map((table) => (
          <FormControlLabel
            key={table}
            control={
              <Checkbox
                checked={selectedTables.includes(table)}
                onChange={() => handleCheckboxChange(table)}
              />
            }
            label={table}
          />
        ))}
      </div>
      <Button variant="contained" onClick={nextStep}>
        Start
      </Button>
      <Button variant="outlined" onClick={nextStep}>
        Skip
      </Button>
    </div>
  );
};

export default SelectTables;