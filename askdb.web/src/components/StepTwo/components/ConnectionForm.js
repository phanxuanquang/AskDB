import React from 'react';
import { Grid, TextField } from '@mui/material';
import AdvancedOptions from './AdvancedOptions';

const ConnectionForm = ({ dbType, formData, errors, onInputChange }) => {
  const textFieldSx = {
    '& .MuiOutlinedInput-root': {
      borderRadius: 1,
      backgroundColor: 'background.paper',
      transition: 'all 0.2s ease-in-out',
      '&:hover': {
        backgroundColor: 'action.hover',
      }
    },
    '& .MuiInputLabel-root': {
      letterSpacing: '0.01em'
    },
    '& .MuiOutlinedInput-input': {
      py: 1.5
    }
  };

  return (
    <Grid container spacing={3}>
      {dbType !== "sqlite" && (
        <>
          <Grid item xs={12} md={8}>
            <TextField
              fullWidth
              size="small"
              label="Server"
              value={formData.host}
              onChange={onInputChange("host")}
              error={!!errors.host}
              helperText={errors.host}
              variant="standard"
              sx={textFieldSx}
            />
          </Grid>
          <Grid item xs={12} md={4}>
            <TextField
              fullWidth
              size="small"
              label="Port"
              value={formData.port}
              onChange={onInputChange("port")}
              error={!!errors.port}
              helperText={errors.port}
              variant="standard"
              sx={textFieldSx}
            />
          </Grid>
        </>
      )}
      <Grid item xs={12}>
        <TextField
          fullWidth
          size="small"
          label={dbType === "sqlite" ? "Database File Path" : "Database Name"}
          value={formData.database}
          onChange={onInputChange("database")}
          error={!!errors.database}
          helperText={errors.database}
          variant="standard"
          sx={textFieldSx}
        />
      </Grid>
      {dbType !== "sqlite" && (
        <>
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              size="small"
              label="Username"
              value={formData.username}
              onChange={onInputChange("username")}
              error={!!errors.username}
              helperText={errors.username}
              variant="standard"
              sx={textFieldSx}
            />
          </Grid>
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              size="small"
              type="password"
              label="Password"
              value={formData.password}
              onChange={onInputChange("password")}
              error={!!errors.password}
              helperText={errors.password}
              variant="standard"
              sx={textFieldSx}
            />
          </Grid>
        </>
      )}
      <Grid item xs={12}>
        <AdvancedOptions
          dbType={dbType}
          formData={formData}
          onInputChange={onInputChange}
        />
      </Grid>
    </Grid>
  );
};

export default ConnectionForm;