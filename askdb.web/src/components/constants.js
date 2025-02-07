export const STEPS = ["Select Database", "Configure Connection", "Connect"];

export const BACKEND_DOMAIN = "https://localhost:5000";

export const DEFAULT_PORTS = {
  mysql: "3306",
  postgresql: "5432",
  sqlserver: "1433",
  sqlite: "",
};

export const DB_TYPES = [
  { value: "sqlserver", label: "SQL Server" },
  { value: "mysql", label: "MySQL" },
  { value: "postgresql", label: "PostgreSQL" },
  { value: "sqlite", label: "SQLite" },
];

export const getDbTypeNumber = (type) => {
  switch (type) {
    case "sqlserver": return 1;
    case "postgresql": return 2;
    case "mysql": return 3;
    case "sqlite": return 4;
    default: return 1;
  }
};

export const DEFAULT_FORM_DATA = {
  host: "",
  port: "",
  database: "",
  username: "",
  password: "",
  ssl: false,
  timeout: "30",
  authMethod: "default",
};