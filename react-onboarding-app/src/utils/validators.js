function isValidApiKey(apiKey) {
  // Basic validation for API key format
  const apiKeyPattern = /^[A-Za-z0-9-_]{32}$/; // Example pattern
  return apiKeyPattern.test(apiKey);
}

function isValidConnectionString(connectionString) {
  // Basic validation for connection string format
  return connectionString && connectionString.trim().length > 0;
}

function isNotEmpty(value) {
  return value && value.trim().length > 0;
}

export { isValidApiKey, isValidConnectionString, isNotEmpty };