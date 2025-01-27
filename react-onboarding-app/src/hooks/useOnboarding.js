import { useState } from 'react';

const useOnboarding = () => {
  const [currentStep, setCurrentStep] = useState(0);
  const [apiKey, setApiKey] = useState('');
  const [databaseType, setDatabaseType] = useState('');
  const [connectionString, setConnectionString] = useState('');
  const [selectedTables, setSelectedTables] = useState([]);

  const nextStep = () => {
    setCurrentStep((prevStep) => Math.min(prevStep + 1, 3)); // Assuming 4 steps
  };

  const previousStep = () => {
    setCurrentStep((prevStep) => Math.max(prevStep - 1, 0));
  };

  const handleApiKeyChange = (key) => {
    setApiKey(key);
  };

  const handleDatabaseTypeChange = (type) => {
    setDatabaseType(type);
  };

  const handleConnectionStringChange = (string) => {
    setConnectionString(string);
  };

  const handleTableSelection = (table) => {
    setSelectedTables((prevSelected) => {
      if (prevSelected.includes(table)) {
        return prevSelected.filter((t) => t !== table);
      } else {
        return [...prevSelected, table];
      }
    });
  };

  return {
    currentStep,
    apiKey,
    databaseType,
    connectionString,
    selectedTables,
    nextStep,
    previousStep,
    handleApiKeyChange,
    handleDatabaseTypeChange,
    handleConnectionStringChange,
    handleTableSelection,
  };
};

export default useOnboarding;