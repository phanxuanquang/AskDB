import React, { useState, useEffect } from "react";
import { ThemeProvider } from "@mui/material/styles";
import { CssBaseline, IconButton, Tab, Tabs, TextField } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import CloseIcon from "@mui/icons-material/Close";
import theme from "./theme";
import LoadingScreen from "./components/LoadingScreen";
import OnboardingModal from "./components/OnboardingModal";
import { Box } from "@mui/material";

import "./App.css";

const TabLabel = ({ index, title, isEditing, onStartEdit, onFinishEdit }) => {
  const [editedTitle, setEditedTitle] = useState(title);

  const handleSubmit = (e) => {
    e.preventDefault();
    onFinishEdit(editedTitle);
  };

  if (isEditing) {
    return (
      <form onSubmit={handleSubmit} onClick={e => e.stopPropagation()}>
        <TextField
          size="small"
          variant="standard"
          placeholder="Tab title"
          autoFocus
          value={editedTitle}
          onChange={(e) => setEditedTitle(e.target.value)}
          onBlur={() => onFinishEdit(editedTitle)}
          sx={{ 
            width: 100,
            maxWidth: '100%'
          }}
        />
      </form>
    );
  }

  return (
    <Box 
      onDoubleClick={(e) => {
        e.stopPropagation();
        onStartEdit();
      }}
      sx={{ 
        display: 'flex', 
        alignItems: 'center',
        maxWidth: 150,
        overflow: 'hidden',
        textOverflow: 'ellipsis',
        whiteSpace: 'nowrap'
      }}
    >
      {title || `Tab ${index + 1}`}
    </Box>
  );
};

const TabBar = ({ tabs, activeTab, onTabChange, onAddTab, onCloseTab, onRenameTab }) => {
  const [editingTabId, setEditingTabId] = useState(null);
  const [hoveredTabId, setHoveredTabId] = useState(null);

  return (
    <Box sx={{ 
      display: 'flex', 
      alignItems: 'center', 
      backgroundColor: 'background.paper',
      borderBottom: 1,
      borderColor: 'divider',
      position: 'fixed',
      top: 0,
      left: 0,
      right: 0,
      zIndex: 1100,
    }}>
      <Tabs 
        value={activeTab}
        onChange={(e, newValue) => onTabChange(newValue)}
        variant="scrollable"
        scrollButtons="auto"
        sx={{ flex: 1 }}
        
      >
        {tabs.map((tab, index) => (
          <Tab
            key={tab.id}
            onMouseEnter={() => setHoveredTabId(tab.id)}
            onMouseLeave={() => setHoveredTabId(null)}
            sx={{
              minWidth: 100,
              maxWidth: 200,
              padding: '6px 12px'
            }}
            label={
              <Box sx={{ 
                display: 'flex', 
                alignItems: 'center',
                justifyContent: 'space-between',
                width: '100%'
              }}>
                <TabLabel
                  index={index}
                  title={tab.title}
                  isEditing={editingTabId === tab.id}
                  onStartEdit={() => setEditingTabId(tab.id)}
                  onFinishEdit={(newTitle) => {
                    onRenameTab(tab.id, newTitle);
                    setEditingTabId(null);
                  }}
                />
                {tabs.length > 1 && hoveredTabId === tab.id && (
                  <IconButton 
                    size="small" 
                    onClick={(e) => {
                      e.stopPropagation();
                      onCloseTab(tab.id);
                    }}
                    sx={{ 
                      ml: 1,
                      opacity: 0.7,
                      '&:hover': {
                        opacity: 1
                      }
                    }}
                  >
                    <CloseIcon fontSize="small" />
                  </IconButton>
                )}
              </Box>
            }
          />
        ))}
      </Tabs>
      <IconButton onClick={onAddTab} sx={{ mr: 1 }}>
        <AddIcon />
      </IconButton>
    </Box>
  );
};

function App() {
  const [isLoading, setIsLoading] = useState(true);
  const [tabs, setTabs] = useState([{ id: 1, title: '' }]);
  const [activeTab, setActiveTab] = useState(0);

  useEffect(() => {
    setTimeout(() => {
      setIsLoading(false);
    }, 1500);
  }, []);

  const handleAddTab = () => {
    const newTab = {
      id: Date.now(),
      title: ''
    };
    setTabs([...tabs, newTab]);
    setActiveTab(tabs.length);
  };

  const handleCloseTab = (tabId) => {
    if (tabs.length <= 1) return;
    
    const tabIndex = tabs.findIndex(tab => tab.id === tabId);
    const newTabs = tabs.filter(tab => tab.id !== tabId);
    setTabs(newTabs);
    
    if (activeTab >= tabIndex && activeTab > 0) {
      setActiveTab(activeTab - 1);
    }
  };

  const handleRenameTab = (tabId, newTitle) => {
    setTabs(tabs.map(tab => 
      tab.id === tabId ? { ...tab, title: newTitle } : tab
    ));
  };

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Box
        sx={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          backgroundImage: `url(https://static.vecteezy.com/system/resources/previews/002/653/245/non_2x/abstract-blur-colorful-background-blurred-background-vector.jpg)`,
          backgroundSize: 'cover',
          backgroundPosition: 'center',
          zIndex: -1
        }}
      />
      {!isLoading && (
        <TabBar
          tabs={tabs}
          activeTab={activeTab}
          onTabChange={setActiveTab}
          onAddTab={handleAddTab}
          onCloseTab={handleCloseTab}
          onRenameTab={handleRenameTab}
        />
      )}
      <Box
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
          tabs.map((tab, index) => (
            activeTab === index && (
              <OnboardingModal 
                key={tab.id}
                onClose={() => handleCloseTab(tab.id)} 
              />
            )
          ))
        )}
      </Box>
    </ThemeProvider>
  );
}

export default App;
