import React, { useState, useEffect } from "react";
import { ThemeProvider } from "@mui/material/styles";
import { CssBaseline } from "@mui/material";
import theme from "./theme";
import LoadingScreen from "./components/LoadingScreen";
import OnboardingModal from "./components/OnboardingModal";
import QueryPanel from "./components/MainPanel/QueryPanel";
import {
  Grid,
  Typography,
  Link,
  Box,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
  Button,
} from "@mui/material";

function App() {
  const [isLoading, setIsLoading] = useState(true);
  const [tabs, setTabs] = useState([{ id: 1, title: "" }]);
  const [activeTab, setActiveTab] = useState(0);
  const [errorDialogOpen, setErrorDialogOpen] = useState(false);
  const [serverAvailable, setServerAvailable] = useState(true);
  const [showQueryPanel, setShowQueryPanel] = useState(false);

  useEffect(() => {
    const checkServerHealth = async () => {
      try {
        const response = await fetch(
          "https://localhost:5000/Authentication/Healthcheck",
          {
            method: "POST",
            headers: {
              Accept: "*/*",
            },
          }
        );

        if (!response.ok) {
          throw new Error("Server health check failed");
        }
        setServerAvailable(true);
      } catch (error) {
        setErrorDialogOpen(true);
        setServerAvailable(false);
      }
    };

    checkServerHealth();

    setTimeout(() => {
      setIsLoading(false);
    });
  }, []);

  const handleCloseErrorDialog = () => {
    setErrorDialogOpen(false);
    setServerAvailable(false); // Keep components disabled after dialog close
  };

  const handleCloseTab = (tabId) => {
    if (tabs.length <= 1) return;

    const tabIndex = tabs.findIndex((tab) => tab.id === tabId);
    const newTabs = tabs.filter((tab) => tab.id !== tabId);
    setTabs(newTabs);

    if (activeTab >= tabIndex && activeTab > 0) {
      setActiveTab(activeTab - 1);
    }
  };

  const handleOnboardingComplete = () => {
    setShowQueryPanel(true);
  };

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Box
        sx={{
          position: "fixed",
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          backgroundImage: `url(https://static.vecteezy.com/system/resources/previews/002/653/245/non_2x/abstract-blur-colorful-background-blurred-background-vector.jpg)`,
          backgroundSize: "cover",
          backgroundPosition: "center",
          zIndex: -1,
        }}
      />
      <Box
        sx={{
          height: "100vh",
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
          tabs.map(
            (tab, index) =>
              activeTab === index && (
                <Box key={tab.id} sx={{ gap: 2, display: "flex", flexDirection: "column" }}>
                  <Grid
                    container
                    sx={{
                      background: "white",
                      textAlign: "center",
                      borderRadius: 3,
                      py: 1,
                    }}
                  >
                    <Grid item xs={12}>
                      <Typography
                        variant="h2"
                        gutterBottom
                        margin={0}
                        style={{
                          textDecoration: "none",
                          fontWeight: "bold",
                          background:
                            "linear-gradient(45deg, #f705cb, #05f7f7)",
                          WebkitBackgroundClip: "text",
                          WebkitTextFillColor: "transparent",
                          display: "inline-block",
                        }}
                      >
                        AskDB
                      </Typography>
                    </Grid>

                    <Grid item xs={12}>
                      <Typography variant="body1" gutterBottom margin={0}>
                        Find the project on my{" "}
                        <Link
                          href="https://github.com/phanxuanquang/XCan-AI"
                          target="_blank"
                          rel="noopener noreferrer"
                          style={{
                            textDecoration: "none",
                            fontWeight: "bold",
                            pointerEvents: !serverAvailable ? "none" : "auto",
                            opacity: !serverAvailable ? 0.5 : 1,
                          }}
                        >
                          GitHub repository
                        </Link>
                      </Typography>
                    </Grid>
                  </Grid>
                  {showQueryPanel ? (
                    <QueryPanel />
                  ) : (
                    <OnboardingModal
                      onClose={() => handleCloseTab(tab.id)}
                      onComplete={handleOnboardingComplete}
                      disabled={!serverAvailable}
                    />
                  )}
                </Box>
              )
          )
        )}
      </Box>

      <Dialog
        open={errorDialogOpen}
        onClose={handleCloseErrorDialog}
        aria-labelledby="error-dialog-title"
        aria-describedby="error-dialog-description"
      >
        <DialogTitle color="error" fontWeight={700} id="error-dialog-title">
          {"Connection Error"}
        </DialogTitle>
        <DialogContent>
          <DialogContentText id="error-dialog-description">
            Unable to connect to the server. Please ensure the backend server is
            running and try again.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button
            onClick={handleCloseErrorDialog}
            variant="contained"
            color="error"
            autoFocus
          >
            Close
          </Button>
        </DialogActions>
      </Dialog>
    </ThemeProvider>
  );
}

export default App;
