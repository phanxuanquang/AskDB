import React, { useState } from "react";
import {
  Box,
  TextField,
  Typography,
  List,
  ListItem,
  ListItemIcon,
  Checkbox,
  ListItemText,
  Button,
  CircularProgress,
  FormControlLabel,
  Alert,
} from "@mui/material";
import { useTheme, useMediaQuery } from "@mui/material";

function TableSelectionStep({ onComplete, onSkip, initialData }) {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));
  const [searchTerm, setSearchTerm] = useState("");
  const [loading, setLoading] = useState(false);

  const [selectedTables, setSelectedTables] = useState(
    initialData.selectedTables || []
  );

  // Use tables from API response and filter directly
  const tables = initialData.tables || [];
  const filteredTables = tables.filter((table) =>
    table.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const handleToggle = (table) => {
    const newSelected = selectedTables.includes(table)
      ? selectedTables.filter((t) => t !== table)
      : [...selectedTables, table];
    setSelectedTables(newSelected);
  };

  const handleSelectAll = (event) => {
    if (event.target.checked) {
      setSelectedTables(filteredTables);
    } else {
      setSelectedTables([]);
    }
  };

  const isAllSelected =
    filteredTables.length > 0 &&
    filteredTables.every((table) => selectedTables.includes(table));
  const isIndeterminate =
    selectedTables.length > 0 && selectedTables.length < filteredTables.length;

  return (
    <Box sx={{ 
      p: { xs: 1, sm: 2 },
      maxWidth: '100%',
      margin: '0 auto'
    }}>
      <Typography 
        variant="h5" 
        mb={2} 
        fontWeight={700}
        sx={{
          fontSize: { xs: '1.25rem', sm: '1.5rem' }
        }}
      >
        Select tables to analyze
      </Typography>
      <Typography 
        variant="body2" 
        mb={3} 
        color="text.secondary"
        sx={{
          fontSize: { xs: '0.875rem', sm: '1rem' }
        }}
      >
        To get started, please enter your Google Gemini API key. You can find or
        create your API key in the.
      </Typography>
      
      <TextField
        fullWidth
        label="Search for tables"
        value={searchTerm}
        onChange={(e) => setSearchTerm(e.target.value)}
        sx={{ 
          mb: 2,
          '& .MuiInputBase-root': {
            fontSize: { xs: '0.875rem', sm: '1rem' }
          }
        }}
      />

      <FormControlLabel
        control={
          <Checkbox
            checked={isAllSelected}
            indeterminate={isIndeterminate}
            onChange={handleSelectAll}
          />
        }
        label="Select all"
        sx={{
          '& .MuiFormControlLabel-label': {
            fontSize: { xs: '0.875rem', sm: '1rem' }
          }
        }}
      />

      <List sx={{ 
        maxHeight: { xs: '30vh', sm: 300 }, 
        overflow: "auto", 
        position: "relative",
        border: 1,
        borderColor: 'divider',
        borderRadius: 1,
        mt: 1
      }}>
        {filteredTables.length === 0 ? (
          <Box
            sx={{
              display: "flex",
              justifyContent: "center",
              alignItems: "center",
              height: 100,
              color: "text.secondary",
            }}
          >
            <Typography>No tables found</Typography>
          </Box>
        ) : (
          filteredTables.map((table) => (
            <ListItem
              key={table}
              dense
              button
              onClick={() => handleToggle(table)}
            >
              <ListItemIcon>
                <Checkbox checked={selectedTables.includes(table)} />
              </ListItemIcon>
              <ListItemText primary={table} />
            </ListItem>
          ))
        )}
      </List>

      {selectedTables.length > 10 && (
        <Alert severity="warning" sx={{ mb: 2, mt: 2 }}>
          Selecting more than 10 tables may affect the accuracy of query suggestions.
        </Alert>
      )}

      <Box sx={{ 
        display: 'flex', 
        flexDirection: { xs: 'column', sm: 'row' },
        justifyContent: 'flex-end', 
        mt: 3, 
        gap: 1 
      }}>
        <Button 
          onClick={onSkip}
          fullWidth={isMobile}
          sx={{ order: { xs: 2, sm: 1 } }}
        >
          I don't need query suggestion.
        </Button>
        <Button
          variant="contained"
          onClick={() => onComplete({ selectedTables })}
          disabled={loading}
          endIcon={loading ? <CircularProgress size={20} color="inherit" /> : null}
          size="large"
          disableElevation
          fullWidth={isMobile}
          sx={{ order: { xs: 1, sm: 2 } }}
        >
          {loading ? "Analyzing..." : "Start"}
        </Button>
      </Box>
    </Box>
  );
}

export default TableSelectionStep;
