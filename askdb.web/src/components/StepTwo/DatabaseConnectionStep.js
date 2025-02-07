import React from "react";
import { Box, Typography, FormControlLabel, Checkbox, Stepper, Step, StepLabel } from "@mui/material";
import { STEPS } from "../constants";
import useConnectionForm from "./useConnectionForm";
import DatabaseTypeSelect from "./components/DatabaseTypeSelect";
import ConnectionForm from "./components/ConnectionForm";
import ErrorDialog from "./components/ErrorDialog";
import StepNavigation from "./components/StepNavigation";

function DatabaseConnectionStep({ onNext, initialData }) {
  const {
    activeStep,
    dbType,
    loading,
    connectionTested,
    formData,
    errors,
    rememberConnection,
    errorDialog,
    handleInputChange,
    handleDbTypeChange,
    handleTestConnection,
    handleNext,
    handleBack,
    closeError,
    setRememberConnection,
  } = useConnectionForm(initialData);

  const renderStepContent = () => {
    switch (activeStep) {
      case 0:
        return (
          <DatabaseTypeSelect
            value={dbType}
            onChange={handleDbTypeChange}
            error={errors.dbType}
          />
        );

      case 1:
      case 2:
        return (
          <ConnectionForm
            dbType={dbType}
            formData={formData}
            errors={errors}
            onInputChange={handleInputChange}
          />
        );

      default:
        return null;
    }
  };

  const handleStepAction = async () => {
    if (activeStep === 1) {
      const tables = await handleTestConnection();
      if (tables) {
        const result = handleNext(tables);
        if (result) {
          onNext(result);
        }
      }
    } else {
      handleNext();
    }
  };

  return (
    <Box sx={{ p: { xs: 1, sm: 2 } }}>
      <Typography variant="h5" mb={2} fontWeight={700}>
        Connect to your database
      </Typography>
      <Typography
        variant="body2"
        mb={3}
        color="text.secondary"
        sx={{ wordBreak: "break-word" }}
      >
        AskDB needs to connect to your database to analyze its structure and execute database queries. 
        Follow the steps below to configure your database connection.
      </Typography>

      <Stepper activeStep={activeStep} sx={{ mb: 4 }}>
        {STEPS.map((label) => (
          <Step key={label}>
            <StepLabel>{label}</StepLabel>
          </Step>
        ))}
      </Stepper>

      {renderStepContent()}

      <FormControlLabel
        control={
          <Checkbox
            checked={rememberConnection}
            onChange={(e) => setRememberConnection(e.target.checked)}
          />
        }
        label="Remember connection credentials"
        sx={{ mt: 2 }}
      />

      <StepNavigation
        activeStep={activeStep}
        loading={loading}
        connectionTested={connectionTested}
        onBack={handleBack}
        onNext={handleStepAction}
        showSuccessMessage={true}
      />

      <ErrorDialog
        open={errorDialog.open}
        message={errorDialog.message}
        onClose={closeError}
      />
    </Box>
  );
}

export default DatabaseConnectionStep;
