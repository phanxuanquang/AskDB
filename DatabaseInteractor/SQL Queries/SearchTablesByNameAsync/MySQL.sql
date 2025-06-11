SELECT 
  table_name 
FROM 
  information_schema.tables 
WHERE 
  table_schema = DATABASE() 
  AND table_type = 'BASE TABLE' 
  AND table_name LIKE CONCAT('%', @keyword, '%')
{MaxResultParam}