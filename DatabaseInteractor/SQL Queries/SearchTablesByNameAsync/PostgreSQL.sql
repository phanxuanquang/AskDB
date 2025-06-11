SELECT 
  quote_ident(table_schema) || '.' || quote_ident(table_name) AS full_table_name 
FROM 
  information_schema.tables 
WHERE 
  table_type = 'BASE TABLE' 
  AND (
    table_schema NOT IN (
      'pg_catalog', 'information_schema'
    )
  ) 
  AND table_name ILIKE @keyword 
ORDER BY 
  table_schema, 
  table_name
{MaxResultParam}