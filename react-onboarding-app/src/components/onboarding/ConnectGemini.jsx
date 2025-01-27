import React, { useState } from 'react';
import { Button, TextField } from '../common';
import { Checkbox, FormControlLabel, Typography } from '@mui/material';

const ConnectGemini = ({ onNext }) => {
  const [apiKey, setApiKey] = useState('');
  const [acceptedPolicy, setAcceptedPolicy] = useState(false);

  const handleNext = () => {
    if (apiKey && acceptedPolicy) {
      onNext(apiKey);
    } else {
      alert('Please enter your API key and accept the policy.');
    }
  };

  return (
    <div>
      <Typography variant="h5">Connect to Google Gemini</Typography>
      <TextField
        label="API Key"
        value={apiKey}
        onChange={(e) => setApiKey(e.target.value)}
        fullWidth
      />
      <FormControlLabel
        control={
          <Checkbox
            checked={acceptedPolicy}
            onChange={(e) => setAcceptedPolicy(e.target.checked)}
          />
        }
        label="I accept the terms and conditions"
      />
      <Button onClick={handleNext} variant="contained" color="primary">
        Next
      </Button>
    </div>
  );
};

export default ConnectGemini;