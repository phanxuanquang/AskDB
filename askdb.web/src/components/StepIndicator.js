import React from 'react';
import { Box, Typography } from '@mui/material';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';

function StepIndicator({ activeStep, totalSteps }) {
  return (
    <Box sx={{ display: 'flex', justifyContent: 'center', mb: 3, gap: 2 }}>
      {Array.from({ length: totalSteps }, (_, index) => index + 1).map((step) => (
        <Box
          key={step}
          sx={{
            width: 30,
            height: 30,
            borderRadius: '50%',
            border: '2px solid',
            borderColor: activeStep >= step ? 'primary.main' : 'grey.400',
            backgroundColor: activeStep >= step ? 'primary.main' : 'white',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          {activeStep > step ? (
            <CheckCircleIcon sx={{ color: 'white', fontSize: 20 }} />
          ) : (
            <Typography variant="body2" color={activeStep >= step ? 'white' : 'grey.700'}>
              {step}
            </Typography>
          )}
        </Box>
      ))}
    </Box>
  );
}

export default StepIndicator;
