import React from 'react';
import { Typography, Button } from '@mui/material';

const Welcome = ({ onNext }) => {
  return (
    <div style={{ textAlign: 'center', padding: '20px' }}>
      <Typography variant="h4" gutterBottom>
        Welcome to the Onboarding Process
      </Typography>
      <Typography variant="body1" paragraph>
        This onboarding flow will guide you through connecting to Google Gemini and setting up your database for analysis.
      </Typography>
      <Button variant="contained" color="primary" onClick={onNext}>
        Get Started
      </Button>
    </div>
  );
};

export default Welcome;