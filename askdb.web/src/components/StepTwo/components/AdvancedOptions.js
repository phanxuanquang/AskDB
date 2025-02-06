import React from 'react';
import {
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Typography,
  Grid,
  FormControlLabel,
  Checkbox,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
} from '@mui/material';
import { ExpandMore } from '@mui/icons-material';

const AdvancedOptions = ({ dbType, formData, onInputChange }) => {
  return (
    <Accordion>
      <AccordionSummary expandIcon={<ExpandMore />}>
        <Typography>Advanced Options</Typography>
      </AccordionSummary>
      <AccordionDetails>
        <Grid container spacing={2}>
          <Grid item xs={12}>
            <FormControlLabel
              control={
                <Checkbox
                  checked={formData.ssl}
                  onChange={onInputChange("ssl")}
                />
              }
              label="Enable SSL/TLS"
            />
          </Grid>
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Connection Timeout (seconds)"
              type="number"
              value={formData.timeout}
              onChange={onInputChange("timeout")}
            />
          </Grid>
          {dbType !== "sqlite" && (
            <Grid item xs={12} md={6}>
              <FormControl fullWidth>
                <InputLabel>Authentication Method</InputLabel>
                <Select
                  value={formData.authMethod}
                  onChange={onInputChange("authMethod")}
                  label="Authentication Method"
                >
                  <MenuItem value="default">Default</MenuItem>
                  <MenuItem value="windows">Windows Authentication</MenuItem>
                  {dbType === "postgresql" && (
                    <MenuItem value="md5">MD5</MenuItem>
                  )}
                </Select>
              </FormControl>
            </Grid>
          )}
        </Grid>
      </AccordionDetails>
    </Accordion>
  );
};

export default AdvancedOptions;