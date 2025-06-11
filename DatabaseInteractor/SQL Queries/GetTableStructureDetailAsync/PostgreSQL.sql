SELECT 
  a.attname AS column_name, 
  pg_catalog.format_type(a.atttypid, a.atttypmod) AS data_type, 
  NOT a.attnotnull AS is_nullable, 
  pg_get_expr(ad.adbin, ad.adrelid) AS column_default, 
  CASE WHEN ct.contype = 'p' THEN 'PRIMARY KEY' WHEN ct.contype = 'f' THEN 'FOREIGN KEY' WHEN ct.contype = 'u' THEN 'UNIQUE' WHEN ct.contype = 'c' THEN 'CHECK' ELSE NULL END AS constraint_type, 
  fk_ns.nspname AS foreign_table_schema, 
  fk_cls.relname AS foreign_table_name, 
  fk_att.attname AS foreign_column_name 
FROM 
  pg_attribute a 
  JOIN pg_class cls ON cls.oid = a.attrelid 
  JOIN pg_namespace ns ON ns.oid = cls.relnamespace 
  LEFT JOIN pg_attrdef ad ON ad.adrelid = a.attrelid 
  AND ad.adnum = a.attnum 
  LEFT JOIN pg_constraint ct ON ct.conrelid = a.attrelid 
  AND a.attnum = ANY (ct.conkey) 
  LEFT JOIN pg_class fk_cls ON fk_cls.oid = ct.confrelid 
  LEFT JOIN pg_namespace fk_ns ON fk_ns.oid = fk_cls.relnamespace 
  LEFT JOIN LATERAL unnest(ct.confkey) WITH ORDINALITY AS fk(attnum, ord) ON ct.contype = 'f' 
  LEFT JOIN LATERAL (
    SELECT 
      attname 
    FROM 
      pg_attribute 
    WHERE 
      attrelid = ct.confrelid 
    ORDER BY 
      attnum OFFSET fk.attnum - 1 
    LIMIT 
      1
  ) AS fk_att ON true 
WHERE 
  cls.relname = @table 
  AND ns.nspname = @schema 
  AND a.attnum > 0 
  AND NOT a.attisdropped 
ORDER BY 
  a.attnum