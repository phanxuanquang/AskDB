import React, { useState } from "react";
import {
  Card,
  CardContent,
  Box,
  Fade,
  Stepper,
  Step,
  StepLabel,
} from "@mui/material";
import GeminiConnectionStep from "./StepOne/GeminiConnectionStep";
import DatabaseConnectionStep from "./StepTwo/DatabaseConnectionStep";
import TableSelectionStep from "./StepThree/TableSelectionStep";
import "../App.css";

function OnboardingModal({ onClose }) {
  const [activeStep, setActiveStep] = useState(0); // Changed to 0-based index
  const [formData, setFormData] = useState({
    apiKey: "",
    databaseConfig: {},
    selectedTables: [],
  });

  const steps = ["Connect to Gemini", "Connect Database", "Select Tables"];

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
          p: 1,
          maxWidth: 1080, // Increased to accommodate image
          mx: "auto",
          minHeight: 300,
          display: "flex",
          flexDirection: "row", // Changed to row for side-by-side layout
        }}
      >
        <CardContent
          sx={{ flex: 1, display: "flex", flexDirection: "column", gap: 2 }}
        >
          <Stepper activeStep={activeStep}>
            {steps.map((label) => (
              <Step key={label}>
                <StepLabel>{label}</StepLabel>
              </Step>
            ))}
          </Stepper>
          <Box
            sx={{
              display: "flex",
              flexDirection: "row",
              gap: 2,
              width: "100%",
            }}
          >
            <Box
              sx={{
                flex: "0 0 40%", // Takes 40% of container width
                height: "auto",
                display: "flex",
                justifyContent: "center",
                alignItems: "center",
                backgroundColor: "grey.100",
              }}
            >
              <img
                src="https://placehold.co/1280x1920"
                alt="Onboarding illustration"
                style={{
                  width: "100%",
                  height: "100%",
                  objectFit: "cover",
                  borderRadius: 8,
                }}
              />
            </Box>
            <Box
              sx={{
                flex: "0 0 60%", // Takes 60% of container width
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
