SELECT 
  c.name AS ColumnName, 
  ty.name AS ColumnDataType, 
  c.is_nullable AS IsColumnNullable, 
  c.max_length AS ColumnMaxLength, 
  kc.type_desc AS ConstraintType, 
  ref_s.name AS ReferencedTableSchema, 
  ref_t.name AS ReferencedTable, 
  ref_c.name AS ReferencedColumn 
FROM 
  sys.columns c 
  JOIN sys.tables t ON c.object_id = t.object_id 
  JOIN sys.schemas s ON t.schema_id = s.schema_id 
  JOIN sys.types ty ON c.user_type_id = ty.user_type_id 
  LEFT JOIN sys.index_columns ic ON ic.object_id = t.object_id 
  AND ic.column_id = c.column_id 
  LEFT JOIN sys.key_constraints kc ON kc.parent_object_id = t.object_id 
  AND ic.index_id = kc.unique_index_id 
  LEFT JOIN sys.foreign_key_columns fkc ON fkc.parent_object_id = t.object_id 
  AND fkc.parent_column_id = c.column_id 
  LEFT JOIN sys.foreign_keys fk ON fk.object_id = fkc.constraint_object_id 
  LEFT JOIN sys.tables ref_t ON ref_t.object_id = fkc.referenced_object_id 
  LEFT JOIN sys.schemas ref_s ON ref_s.schema_id = ref_t.schema_id 
  LEFT JOIN sys.columns ref_c ON ref_c.object_id = fkc.referenced_object_id 
  AND ref_c.column_id = fkc.referenced_column_id 
WHERE 
  t.name = @table 
  AND s.name = @schema 
ORDER BY 
  c.column_id
