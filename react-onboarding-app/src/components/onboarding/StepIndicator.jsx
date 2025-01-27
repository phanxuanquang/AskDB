import React from 'react';
import { Step, StepLabel, Stepper } from '@mui/material';

const steps = ['Welcome', 'Connect to Google Gemini', 'Connect to Database', 'Select Tables'];

const StepIndicator = ({ activeStep }) => {
  return (
    <Stepper activeStep={activeStep} alternativeLabel>
      {steps.map((label) => (
        <Step key={label}>
          <StepLabel>{label}</StepLabel>
        </Step>
      ))}
    </Stepper>
  );
};

export default StepIndicator;