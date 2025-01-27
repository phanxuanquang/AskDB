import React from 'react';
import { Box } from '@mui/material';

function ProgressDots({ activeStep, totalSteps }) {
  return (
    <Box sx={{ display: 'flex' }}>
      {Array.from({ length: totalSteps }, (_, index) => index + 1).map((step) => (
        <Box
          key={step}
          sx={{
            width: 8,
            height: 8,
            borderRadius: '50%',
            backgroundColor: activeStep === step ? 'primary.main' : 'grey.300',
            mx: 0.5,
          }}
        />
      ))}
    </Box>
  );
}

export default ProgressDots;