import { useState, useEffect } from 'react';
import { DEFAULT_FORM_DATA, DEFAULT_PORTS } from '../constants';
import { validateForm, buildConnectionString, testConnection } from './connectionUtils';

export const useConnectionForm = (initialData) => {
  const [activeStep, setActiveStep] = useState(0);
  const [dbType, setDbType] = useState(initialData.dbType || "");
  const [loading, setLoading] = useState(false);
  const [connectionTested, setConnectionTested] = useState(false);
  const [formData, setFormData] = useState(DEFAULT_FORM_DATA);
  const [errors, setErrors] = useState({});
  const [rememberConnection, setRememberConnection] = useState(true);
  const [errorDialog, setErrorDialog] = useState({ open: false, message: "" });

  useEffect(() => {
    loadSavedConnection();
  }, []);

  useEffect(() => {
    if (dbType) {
      setFormData(prev => ({
        ...prev,
        port: DEFAULT_PORTS[dbType] || "",
      }));
    }
  }, [dbType]);

  const loadSavedConnection = () => {
    const savedConnection = localStorage.getItem("savedConnection");
    if (savedConnection) {
      const savedData = JSON.parse(savedConnection);
      setDbType(savedData.dbType);
      setFormData(savedData.formData);
    }
  };

  const handleInputChange = (field) => (event) => {
    const value = event.target.type === "checkbox" ? event.target.checked : event.target.value;
    setFormData(prev => ({
      ...prev,
      [field]: value,
    }));
    setErrors(prev => ({
      ...prev,
      [field]: "",
    }));
    setConnectionTested(false);
  };

  const handleDbTypeChange = (newType) => {
    setDbType(newType);
    setErrors(prev => ({ ...prev, dbType: "" }));
    setConnectionTested(false);
  };

  const showError = (message) => {
    setErrorDialog({ open: true, message });
  };

  const closeError = () => {
    setErrorDialog({ open: false, message: "" });
  };

  const handleTestConnection = async () => {
    const validation = validateForm(dbType, formData);
    if (!validation.isValid) {
      setErrors(validation.errors);
      return;
    }

    setLoading(true);
    try {
      const connectionString = buildConnectionString(dbType, formData);
      const tables = await testConnection(dbType, connectionString);
      setConnectionTested(true);
      return tables;
    } catch (error) {
      showError(error.message);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const handleNext = (tables) => {
    if (activeStep === 1 && tables) {
      if (rememberConnection) {
        localStorage.setItem(
          "savedConnection",
          JSON.stringify({ dbType, formData })
        );
      } else {
        localStorage.removeItem("savedConnection");
      }
      // Pass the tables directly to onNext for TableSelectionStep
      return { tables, dbType, connectionString: buildConnectionString(dbType, formData) };
    } else {
      setActiveStep(prev => prev + 1);
      return null;
    }
  };

  const handleBack = () => {
    setActiveStep(prev => prev - 1);
  };

  return {
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
    showError,
    closeError,
    setRememberConnection,
  };
};

export default useConnectionForm;