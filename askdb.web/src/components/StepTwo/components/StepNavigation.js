import React from 'react';
import { Box, Button, CircularProgress } from '@mui/material';
import { ArrowForward } from '@mui/icons-material';

const StepNavigation = ({
  activeStep,
  loading,
  connectionTested,
  onBack,
  onNext,
  showSuccessMessage = false,
}) => {
  return (
    <Box sx={{ mt: 2 }}>
      {showSuccessMessage && connectionTested && (
        <Box
          sx={{
            p: 2,
            mb: 3,
            bgcolor: 'success.light',
            borderRadius: 1,
            color: 'success.dark',
            fontSize: '0.875rem',
            letterSpacing: '0.01em'
          }}
        >
          Connection successful
        </Box>
      )}

      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          gap: 2
        }}
      >
        <Button
          onClick={onBack}
          disabled={activeStep === 0 || loading}
          sx={{
            visibility: activeStep === 0 ? "hidden" : "visible",
            textTransform: 'none',
            letterSpacing: '0.01em',
            px: 3
          }}
        >
          Back
        </Button>
        <Button
          variant="contained"
          onClick={onNext}
          disableElevation
          disabled={loading || (activeStep === 2 && !connectionTested)}
          sx={{
            textTransform: 'none',
            letterSpacing: '0.01em',
            px: 3,
            py: 1
          }}
          endIcon={
            loading ? (
              <CircularProgress size={16} color="inherit" />
            ) : (
              <ArrowForward sx={{ fontSize: 18 }} />
            )
          }
        >
          {loading
            ? "Processing..."
            : activeStep === 2
            ? "Finish"
            : activeStep === 1
            ? "Connect"
            : "Next"}
        </Button>
      </Box>
    </Box>
  );
};

export default StepNavigation;