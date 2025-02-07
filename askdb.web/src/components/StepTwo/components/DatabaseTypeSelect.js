import React from 'react';
import { FormControl, InputLabel, Select, MenuItem } from '@mui/material';
import { DB_TYPES } from '../../constants';

const DatabaseTypeSelect = ({ value, onChange, error }) => {
  return (
    <FormControl
      fullWidth
      error={!!error}
      sx={{
        '& .MuiOutlinedInput-root': {
          borderRadius: 1,
          backgroundColor: 'background.paper',
          transition: 'all 0.2s ease-in-out',
          '&:hover': {
            backgroundColor: 'action.hover',
          }
        },
        '& .MuiSelect-select': {
          py: 1.5
        }
      }}
    >
      <InputLabel sx={{ letterSpacing: '0.01em' }}>Database Type</InputLabel>
      <Select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        label="Database Type"
        variant="standard"
      >
        {DB_TYPES.map(({ value, label }) => (
          <MenuItem
            key={value}
            value={value}
            sx={{
              py: 1.5,
              letterSpacing: '0.01em'
            }}
          >
            {label}
          </MenuItem>
        ))}
      </Select>
    </FormControl>
  );
};

export default DatabaseTypeSelect;