import React, { useState } from 'react';
import { ThemeProvider } from '@mui/material/styles';
import theme from './theme';
import OnboardingContextProvider from './contexts/OnboardingContext';
import Loading from './components/onboarding/Loading';
import Welcome from './components/onboarding/Welcome';
import ConnectGemini from './components/onboarding/ConnectGemini';
import ConnectDatabase from './components/onboarding/ConnectDatabase';
import SelectTables from './components/onboarding/SelectTables';
import StepIndicator from './components/onboarding/StepIndicator';

function App() {
  const [currentStep, setCurrentStep] = useState(0);

  const steps = [
    <Welcome nextStep={() => setCurrentStep(1)} />,
    <ConnectGemini nextStep={() => setCurrentStep(2)} />,
    <ConnectDatabase nextStep={() => setCurrentStep(3)} />,
    <SelectTables completeOnboarding={() => alert('Onboarding Complete!')} />,
  ];

  return (
    <ThemeProvider theme={theme}>
      <OnboardingContextProvider>
        {currentStep === 0 ? <Loading /> : (
          <>
            <StepIndicator currentStep={currentStep} totalSteps={steps.length} />
            {steps[currentStep]}
          </>
        )}
      </OnboardingContextProvider>
    </ThemeProvider>
  );
}

export default App;