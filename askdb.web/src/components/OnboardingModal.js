import React, { useState } from "react";
import {
  Card,
  CardContent,
  Box,
  Fade,
  Stepper,
  Step,
  StepLabel,
  useTheme,
  useMediaQuery,
} from "@mui/material";
import GeminiConnectionStep from "./StepOne/GeminiConnectionStep";
import DatabaseConnectionStep from "./StepTwo/DatabaseConnectionStep";
import TableSelectionStep from "./StepThree/TableSelectionStep";
import "../App.css";

function OnboardingModal({ onClose }) {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));

  const [activeStep, setActiveStep] = useState(0); // Changed to 0-based index
  const [formData, setFormData] = useState({
    apiKey: "",
    databaseConfig: {},
    selectedTables: [],
  });

  const steps = ["Gemini Access", "Database Access", "Query Suggestion"];

  const handleNextStep = (data) => {
    setFormData({ ...formData, ...data });
    setActiveStep((prev) => prev + 1);
  };

  const handleComplete = (data) => {
    const finalData = { ...formData, ...data };
    console.log("Onboarding complete:", finalData);
    onClose();
  };

  const renderStep = () => {
    switch (activeStep) {
      case 0:
        return (
          <GeminiConnectionStep
            onNext={handleNextStep}
            initialData={formData}
          />
        );
      case 1:
        return (
          <DatabaseConnectionStep
            onNext={handleNextStep}
            initialData={formData}
          />
        );
      case 2:
        return (
          <TableSelectionStep
            onComplete={handleComplete}
            onSkip={() => handleComplete({})}
            initialData={formData}
          />
        );
      default:
        return null;
    }
  };

  return (
    <Fade in={true}>
      <Card
        sx={{
          p: { xs: 1, sm: 2, md: 3 },
          maxWidth: { xs: '100%', sm: '90%', md: 1080 },
          width: '100%',
          mx: "auto",
          minHeight: { xs: '90vh', sm: 'auto' },
          display: "flex",
          flexDirection: "column",
          position: 'relative',
          overflow: 'auto',
          maxHeight: { xs: '100vh', sm: '100vh' },
        }}
      >
        <CardContent
          sx={{
            flex: 1,
            display: "flex",
            flexDirection: "column",
            gap: { xs: 2, sm: 3, md: 4 },
            p: { xs: 1, sm: 2 },
          }}
        >
          <Stepper 
            activeStep={activeStep}
            alternativeLabel={isMobile}
            sx={{
              mb: { xs: 1, sm: 0 },
              '& .MuiStepLabel-label': {
                fontSize: { xs: '0.8rem', sm: '1rem' }
              }
            }}
          >
            {steps.map((label) => (
              <Step key={label}>
                <StepLabel>{label}</StepLabel>
              </Step>
            ))}
          </Stepper>
          <Box
            sx={{
              display: "flex",
              flexDirection: { xs: 'column', md: 'row' },
              gap: { xs: 2, sm: 3, md: 4 },
              width: "100%",
            }}
          >
            {!isMobile && (
              <Box
                sx={{
                  flex: { md: '0 0 40%' },
                  display: { xs: 'none', md: 'flex' },
                  justifyContent: "center",
                  alignItems: "center",
                  bgcolor: 'grey.50',
                  borderRadius: 1,
                  overflow: 'hidden',
                  position: 'relative',
                }}
              >
                <img
                  src="https://placehold.co/1280x1920"
                  alt="Onboarding illustration"
                  style={{
                    width: "100%",
                    height: "100%",
                    objectFit: "cover",
                    transition: 'transform 0.3s ease',
                    '&:hover': {
                      transform: 'scale(1.05)',
                    },
                  }}
                  loading="lazy"
                />
              </Box>
            )}
            <Box
              sx={{
                flex: { md: '0 0 60%' },
                width: '100%',
                display: 'flex',
                flexDirection: 'column',
                gap: 2,
              }}
            >
              {renderStep()}
            </Box>
          </Box>
        </CardContent>
      </Card>
    </Fade>
  );
}

export default OnboardingModal;
