SELECT 
  cols.COLUMN_NAME AS ColumnName, 
  cols.DATA_TYPE AS ColumnDataType, 
  cols.IS_NULLABLE AS IsColumnNullable, 
  cols.CHARACTER_MAXIMUM_LENGTH AS ColumnMaxLength, 
  COALESCE(tc.CONSTRAINT_TYPE, '') AS ColumnConstraintType, 
  kcu.REFERENCED_TABLE_NAME AS ReferencedTable, 
  kcu.REFERENCED_COLUMN_NAME AS ReferencedColumn 
FROM 
  information_schema.COLUMNS cols 
  LEFT JOIN information_schema.KEY_COLUMN_USAGE kcu ON cols.TABLE_SCHEMA = kcu.TABLE_SCHEMA 
  AND cols.TABLE_NAME = kcu.TABLE_NAME 
  AND cols.COLUMN_NAME = kcu.COLUMN_NAME 
  LEFT JOIN information_schema.TABLE_CONSTRAINTS tc ON kcu.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA 
  AND kcu.TABLE_NAME = tc.TABLE_NAME 
  AND kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME 
WHERE 
  cols.TABLE_SCHEMA = DATABASE() 
  AND cols.TABLE_NAME = @table 
ORDER BY 
  cols.ORDINAL_POSITION;