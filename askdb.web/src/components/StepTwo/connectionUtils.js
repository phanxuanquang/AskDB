export const validateForm = (dbType, formData) => {
  const errors = {};
  if (!dbType) errors.dbType = "Database type is required";
  
  if (dbType !== "sqlite") {
    if (!formData.host) errors.host = "Host is required";
    if (!formData.port) errors.port = "Port is required";
    if (!formData.database) errors.database = "Database name is required";
    if (!formData.username) errors.username = "Username is required";
    if (!formData.password) errors.password = "Password is required";
  } else {
    if (!formData.database) errors.database = "Database file path is required";
  }

  return {
    isValid: Object.keys(errors).length === 0,
    errors
  };
};

export const buildConnectionString = (dbType, formData) => {
  switch (dbType) {
    case "mysql":
      return `Server=${formData.host};Port=${formData.port};Database=${formData.database};Uid=${formData.username};Pwd=${formData.password};${formData.ssl ? "SslMode=Required;" : ""}ConnectionTimeout=${formData.timeout};`;
    case "postgresql":
      return `Host=${formData.host};Port=${formData.port};Database=${formData.database};Username=${formData.username};Password=${formData.password};${formData.ssl ? "SSL Mode=Require;" : ""}Timeout=${formData.timeout};`;
    case "sqlserver":
      return `Server=${formData.host},${formData.port};Database=${formData.database};User Id=${formData.username};Password=${formData.password};${formData.ssl ? "Encrypt=True;" : ""}Connection Timeout=${formData.timeout};`;
    case "sqlite":
      return `Data Source=${formData.database};`;
    default:
      return "";
  }
};

export const testConnection = async (dbType, connectionString) => {
  try {
    const response = await fetch(
      `https://localhost:5000/api/DatabaseAnalyzer/InitConnection?databaseType=${getDbTypeNumber(dbType)}&connectionStringBox=${connectionString}`,
      {
        method: "POST",
        headers: { accept: "*/*" },
      }
    );

    if (!response.ok) {
      const errorData = await response.text();
      throw new Error(errorData);
    }

    return await response.json();
  } catch (error) {
    throw new Error(`Error connecting to database: ${error.message}`);
  }
};

const getDbTypeNumber = (type) => {
  switch (type) {
    case "sqlserver": return 1;
    case "postgresql": return 2;
    case "mysql": return 3;
    case "sqlite": return 4;
    default: return 1;
  }
};