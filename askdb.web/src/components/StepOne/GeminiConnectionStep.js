import React, { useState } from "react";
import {
  TextField,
  Button,
  FormControlLabel,
  Checkbox,
  Typography,
  Box,
  Link,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  CircularProgress
} from "@mui/material";
import { ArrowForward } from "@mui/icons-material";
import axios from 'axios';

function GeminiConnectionStep({ onNext, initialData }) {
  const [apiKey, setApiKey] = useState(initialData?.apiKey || "");
  const [acceptedPolicies, setAcceptedPolicies] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [openDialog, setOpenDialog] = useState(false);

  const handleSubmit = async () => {
    setLoading(true);
    try {
      const response = await fetch(`https://localhost:5000/Authentication/ValidateGeminiApiKey?apiKey=${apiKey}`, {
        method: 'POST',
        headers: {
          'accept': '*/*'
        }
      });

      if (!response.ok) {
        const errorData = await response.text();
        throw new Error(errorData || 'Failed to validate API key');
      }

      onNext();
      
    } catch (err) {
      setError(err.message);
      setOpenDialog(true);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box sx={{ p: 2, paddingRight: 5 }}>
      <Typography variant="h4" mb={2} fontWeight={700}>
        Connect to Google Gemini
      </Typography>
      <Typography variant="body2" mb={3} color="text.secondary">
        To get started, please enter your Google Gemini API key. You can find or
        create your API key in the{" "}
        <Link
          href="https://makersuite.google.com/app/apikey"
          target="_blank"
          rel="noopener"
        >
          Google AI Studio
        </Link>
        .
      </Typography>

      <TextField
        fullWidth
        label="Gemini API Key"
        value={apiKey}
        onChange={(e) => setApiKey(e.target.value)}
        type="password"
      />

      <FormControlLabel
        control={
          <Checkbox
            checked={acceptedPolicies}
            onChange={(e) => setAcceptedPolicies(e.target.checked)}
          />
        }
        label={
          <Typography variant="body2">
            I accept Google's{" "}
            <Link
              href="https://ai.google.dev/terms"
              target="_blank"
              rel="noopener"
            >
              AI Terms of Service
            </Link>
          </Typography>
        }
        sx={{ mb: 3 }}
      />

      <Box sx={{ display: "flex", justifyContent: "flex-end", mt: 2 }}>
        <Button
          variant="contained"
          onClick={handleSubmit}
          disabled={!apiKey || !acceptedPolicies || loading}
          endIcon={loading ? <CircularProgress size={20} color="inherit" /> : <ArrowForward />}
          size="large"
          disableElevation
        >
          {loading ? 'Validating...' : 'Continue'}
        </Button>
      </Box>

      <Dialog open={openDialog} onClose={() => setOpenDialog(false)}>
        <DialogTitle color="red">Error</DialogTitle>
        <DialogContent sx={{minWidth: 400}} >
          {error}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDialog(false)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

export default GeminiConnectionStep;
