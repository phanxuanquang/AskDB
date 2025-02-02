import React, { useState, useEffect } from "react";
import {
  Box,
  TextField,
  Typography,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Button,
  Grid,
  CircularProgress,
  Checkbox,
  FormControlLabel,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
} from "@mui/material";
import { ArrowForward } from "@mui/icons-material";

function DatabaseConnectionStep({ onNext, initialData }) {
  const [dbType, setDbType] = useState(initialData.dbType || "");
  const [loading, setLoading] = useState(false);
  const [connectionString, setConnectionString] = useState(
    initialData.connectionString || ""
  );
  const [rememberConnection, setRememberConnection] = useState(true);
  const [errorDialog, setErrorDialog] = useState({ open: false, message: "" });

  useEffect(() => {
    const savedConnection = localStorage.getItem("savedConnection");
    if (savedConnection) {
      const { dbType: savedType, connectionString: savedString } =
        JSON.parse(savedConnection);
      setDbType(savedType);
      setConnectionString(savedString);
    }
  }, []);

  const getDbTypeNumber = (type) => {
    switch (type) {
      case "sqlserver":
        return 1;
      case "postgresql":
        return 2;
      case "mysql":
        return 3;
      case "sqlite":
        return 4;
      default:
        return 1;
    }
  };

  const handleCloseError = () => {
    setErrorDialog({ open: false, message: "" });
  };

  const showError = (message) => {
    setErrorDialog({ open: true, message });
  };

  const handleSubmit = async () => {
    setLoading(true);
    if (!dbType || !connectionString) {
      showError("Please fill in all fields");
      setLoading(false);
      return;
    }

    try {
      const response = await fetch(
        `https://localhost:5000/api/DatabaseAnalyzer/InitConnection?databaseType=${getDbTypeNumber(
          dbType
        )}&connectionStringBox=${connectionString}`,
        {
          method: "POST",
          headers: {
            accept: "*/*",
          },
        }
      );

      if (response.ok) {
        const tables = await response.json();
        if (rememberConnection) {
          localStorage.setItem(
            "savedConnection",
            JSON.stringify({ dbType, connectionString })
          );
        } else {
          localStorage.removeItem("savedConnection");
        }
        onNext({ dbType, connectionString, tables });
      } else {
        const errorData = await response.text();
        showError(errorData);
      }
    } catch (error) {
      showError("Error connecting to database: " + error.message);
    }

    setLoading(false);
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
        AskDB needs to connect to your database to analyze its structure and execute database queries. Please provide the connection string for your database. To find the connection string, you can refer to the official documentation of your database provider or contact your database administrator.
      </Typography>
      <Grid container spacing={2} sx={{ mb: 1 }}>
        <Grid item xs={12} md={4}>
          <FormControl fullWidth>
            <InputLabel>Database Type</InputLabel>
            <Select
              value={dbType}
              onChange={(e) => setDbType(e.target.value)}
              label="Database Type"
            >
              <MenuItem value="sqlserver">SQL Server</MenuItem>
              <MenuItem value="mysql">MySQL</MenuItem>
              <MenuItem value="postgresql">PostgreSQL</MenuItem>
              <MenuItem value="sqlite">SQLite</MenuItem>
            </Select>
          </FormControl>
        </Grid>
        <Grid item xs={12} md={8}>
          <TextField
            fullWidth
            label="Connection String"
            value={connectionString}
            onChange={(e) => setConnectionString(e.target.value)}
          />
        </Grid>
      </Grid>
      <FormControlLabel
        control={
          <Checkbox
            checked={rememberConnection}
            onChange={(e) => setRememberConnection(e.target.checked)}
          />
        }
        label="Remember connection credentials."
        sx={{ mb: 1 }}
      />
      <Box
        sx={{
          display: "flex",
          justifyContent: { xs: "center", sm: "flex-end" },
        }}
      >
        <Button
          fullWidth
          sx={{ maxWidth: { xs: "100%", sm: "auto" } }}
          variant="contained"
          onClick={handleSubmit}
          disabled={loading}
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
          {loading ? "Connecting..." : "Continue"}
        </Button>
      </Box>
      <Dialog
        open={errorDialog.open}
        onClose={handleCloseError}
        aria-labelledby="error-dialog-title"
        aria-describedby="error-dialog-description"
      >
        <DialogTitle id="error-dialog-title">Error</DialogTitle>
        <DialogContent>
          <DialogContentText id="error-dialog-description">
            {errorDialog.message}
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseError} color="primary" autoFocus>
            OK
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

export default DatabaseConnectionStep;
