import React, { useState, useEffect } from "react";
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
  CircularProgress,
} from "@mui/material";
import { ArrowForward } from "@mui/icons-material";
import { BACKEND_DOMAIN } from "../constants";

function GeminiConnectionStep({ onNext, initialData }) {
  const [apiKey, setApiKey] = useState(initialData?.apiKey || "");
  const [acceptedPolicies, setAcceptedPolicies] = useState(false);
  const [rememberApiKey, setRememberApiKey] = useState(true);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [openDialog, setOpenDialog] = useState(false);

  useEffect(() => {
    const savedApiKey = localStorage.getItem("geminiApiKey");
    if (savedApiKey) {
      setApiKey(savedApiKey);
    }
  }, []);

  const handleSubmit = async () => {
    setLoading(true);
    try {
      const response = await fetch(
        `${BACKEND_DOMAIN}/Authentication/ValidateGeminiApiKey?apiKey=${apiKey}`,
        {
          method: "POST",
          headers: {
            accept: "*/*",
          },
        }
      );

      if (!response.ok) {
        const errorData = await response.text();
        throw new Error(errorData || "Failed to validate API key.");
      }

      if (rememberApiKey) {
        localStorage.setItem("geminiApiKey", apiKey);
      } else {
        localStorage.removeItem("geminiApiKey");
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
      <Typography variant="h5" mb={2} fontWeight={700}>
        Connect to Gemini
      </Typography>
      <Typography variant="body2" mb={1} color="text.secondary">
        AskDB ultilize Gemini as the AI engine for natural language processing
        purposes. To get started, please enter your Google Gemini API key.
      </Typography>
      <Typography variant="body2" mb={3} color="text.secondary">
        You can find or create your API key in the{" "}
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
        sx={{ mb: 1 }}
      />

      <Box sx={{ mb: 2 }}>
        <FormControlLabel
          control={
            <Checkbox
              checked={acceptedPolicies}
              onChange={(e) => setAcceptedPolicies(e.target.checked)}
            />
          }
          label={
            <Typography variant="body2">
              I accept the{" "}
              <Link
                href="https://ai.google.dev/terms"
                target="_blank"
                rel="noopener"
              >
                Terms of Service
              </Link>
              .
            </Typography>
          }
        />

        <FormControlLabel
          control={
            <Checkbox
              checked={rememberApiKey}
              onChange={(e) => setRememberApiKey(e.target.checked)}
            />
          }
          label={<Typography variant="body2">Remember API Key.</Typography>}
        />
      </Box>

      <Box>
        <Button
          variant="contained"
          fullWidth
          onClick={handleSubmit}
          disabled={!apiKey || !acceptedPolicies || loading}
          endIcon={
            loading ? (
              <CircularProgress size={20} color="inherit" />
            ) : (
              <ArrowForward />
            )
          }
          size="large"
          disableElevation
        >
          {loading ? "Validating..." : "Continue"}
        </Button>
      </Box>

      <Dialog open={openDialog} onClose={() => setOpenDialog(false)}>
        <DialogTitle>Error</DialogTitle>
        <DialogContent>{error}</DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDialog(false)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

export default GeminiConnectionStep;
