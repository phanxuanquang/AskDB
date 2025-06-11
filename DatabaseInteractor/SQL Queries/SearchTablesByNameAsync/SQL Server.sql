SELECT {MaxResultParam} 
	'[' + table_schema + '].[' + table_name + ']' as TableFullName
FROM information_schema.tables 
WHERE table_type = 'BASE TABLE' 
	AND TABLE_SCHEMA 
		NOT IN ( 'sys', 'INFORMATION_SCHEMA', 'db_owner', 'db_accessadmin', 'db_securityadmin', 'db_backupoperator', 'db_ddladmin', 'db_datareader', 'db_datawriter', 'db_denydatareader', 'db_denydatawriter') 
	AND table_name LIKE @keyword
ORDER BY table_schema, table_name;