SELECT 
  name as ColumnName, 
  type as DataType, 
  pk as IsPrimaryKey,
  `notnull` as IsNullable, 
  dflt_value as DefaultValue
FROM 
  pragma_table_info('{TableName}');