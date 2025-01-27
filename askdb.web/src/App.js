import React, { useState, useEffect } from "react";
import { ThemeProvider } from "@mui/material/styles";
import { CssBaseline } from "@mui/material";
import theme from "./theme";
import LoadingScreen from "./components/LoadingScreen";
import OnboardingModal from "./components/OnboardingModal";
import { Box } from "@mui/material";

import "./App.css";

function App() {
  const [isLoading, setIsLoading] = useState(true);
  const [showOnboarding, setShowOnboarding] = useState(false);

  useEffect(() => {
    // Simulate initial loading
    setTimeout(() => {
      setIsLoading(false);
      setShowOnboarding(true);
    }, 1500);
  }, []);

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Box
        className="animated-gradient"
        sx={{
          minHeight: "100vh",
          alignItems: "center",
          justifyContent: "center",
          py: 3,
          display: "flex",
          flexDirection: "column",
          position: "relative",
        }}
      >
        {isLoading ? (
          <LoadingScreen />
        ) : (
          showOnboarding && (
            <OnboardingModal onClose={() => setShowOnboarding(false)} />
          )
        )}
      </Box>
    </ThemeProvider>
  );
}

export default App;
