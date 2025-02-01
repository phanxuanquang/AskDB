import React, { useState } from "react";
import {
  Box,
  TextField,
  Typography,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Button,
  Link,
  Grid,
  CircularProgress,
} from "@mui/material";
import { ArrowForward } from "@mui/icons-material";

function DatabaseConnectionStep({ onNext, initialData }) {
  const [dbType, setDbType] = useState(initialData.dbType || "");
  const [loading, setLoading] = useState(false);

  const [connectionString, setConnectionString] = useState(
    initialData.connectionString || ""
  );

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

  const handleSubmit = async () => {
    setLoading(true);
    if (!dbType || !connectionString) {
      alert("Please fill in all fields");
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
        onNext({ dbType, connectionString, tables });
      } else {
        alert("Failed to connect to database");
      }
    } catch (error) {
      alert("Error connecting to database: " + error.message);
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
        sx={{ wordBreak: 'break-word' }}
      >
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
      <Grid container spacing={2} sx={{ mb: 3 }}>
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
      <Box sx={{ 
        display: 'flex', 
        justifyContent: { xs: 'center', sm: 'flex-end' }, 
        mt: 2 
      }}>
        <Button
          fullWidth
          sx={{ maxWidth: { xs: '100%', sm: 'auto' } }}
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
          {loading ? "Validating..." : "Continue"}
        </Button>
      </Box>
    </Box>
  );
}

export default DatabaseConnectionStep;
