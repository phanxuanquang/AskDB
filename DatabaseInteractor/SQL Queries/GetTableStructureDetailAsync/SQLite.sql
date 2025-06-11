SELECT 
  name as ColumnName, 
  type as DataType, 
  pk as IsPrimaryKey,
  `notnull` as IsNullable
FROM 
  pragma_table_info('{TableName}');