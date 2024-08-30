namespace DatabaseAnalyzer
{
    public class DatabaseConstant
    {
        public static readonly string[] SqlServerKeywords = new string[]
        {
            "SELECT", "FROM", "WHERE", "JOIN", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN",
            "FULL JOIN", "ON", "GROUP BY", "ORDER BY", "HAVING", "DISTINCT", "UNION",
            "UNION ALL", "LIMIT", "OFFSET", "AS", "IN", "NOT IN", "BETWEEN", "AND", "OR",
            "LIKE", "IS NULL", "IS NOT NULL", "ROW_NUMBER", "RANK", "DENSE_RANK", "NTILE",
            "PARTITION BY", "OVER", "CROSS JOIN", "SELF JOIN", "COALESCE", "NULLIF", "SUBSTRING",
            "LEN", "LEFT", "RIGHT", "CHAR", "CHARINDEX", "LOWER", "UPPER", "LTRIM", "RTRIM",
            "REPLACE", "REPLICATE", "REVERSE", "STUFF", "ABS", "AVG", "CEILING", "COUNT",
            "FLOOR", "MAX", "MIN", "ROUND", "SUM", "DATEDIFF", "DATEADD", "DATEPART",
            "GETDATE", "GETUTCDATE", "ISDATE", "CAST", "CONVERT", "STRING_AGG", "XML PATH",
            "FOR XML", "NEWID", "CURRENT_TIMESTAMP", "OPENQUERY", "OPENROWSET", "OPENDATASOURCE",
            "CROSS APPLY", "OUTER APPLY", "MERGE", "WITH (NOLOCK)", "WITH (READUNCOMMITTED)",
            "FORCESEEK", "FORCESCAN", "INDEX", "INCLUDE", "COMPUTE", "COMPUTE BY", "TRY_CONVERT",
            "TRY_CAST", "TRY_PARSE", "CASE", "WHEN", "THEN", "ELSE", "END", "EXISTS",
            "NOT EXISTS", "ANY", "ALL", "SOME", "FORMAT", "OPTIMIZE", "EXECUTION", "PLAN",
            "RECOMPILE", "SEQUENCE", "NEXT VALUE FOR"
        };

        public static readonly string[] MySqlKeywords = new string[]
        {
            "SELECT", "FROM", "WHERE", "JOIN", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN",
            "ON", "GROUP BY", "ORDER BY", "HAVING", "DISTINCT", "UNION", "UNION ALL",
            "LIMIT", "OFFSET", "AS", "IN", "NOT IN", "BETWEEN", "AND", "OR", "LIKE",
            "IS NULL", "IS NOT NULL", "COALESCE", "NULLIF", "SUBSTRING", "LENGTH", "CHAR",
            "LOWER", "UPPER", "LTRIM", "RTRIM", "REPLACE", "REVERSE", "CONCAT", "ABS",
            "AVG", "CEILING", "COUNT", "FLOOR", "MAX", "MIN", "ROUND", "SUM", "DATEDIFF",
            "DATE_ADD", "DATE_SUB", "DATE_FORMAT", "NOW", "CURDATE", "CURTIME", "CURRENT_TIMESTAMP",
            "DAY", "MONTH", "YEAR", "HOUR", "MINUTE", "SECOND", "WEEK", "WEEKDAY", "ISNULL",
            "CAST", "CONVERT", "GROUP_CONCAT", "EXISTS", "NOT EXISTS", "ANY", "ALL", "SOME",
            "REGEXP", "FIND_IN_SET", "RAND", "TRUNCATE", "GREATEST", "LEAST"
        };

        public static readonly string[] PostgreSqlKeywords = new string[]
        {
            "SELECT", "FROM", "WHERE", "JOIN", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN",
            "FULL JOIN", "ON", "GROUP BY", "ORDER BY", "HAVING", "DISTINCT", "UNION",
            "UNION ALL", "LIMIT", "OFFSET", "AS", "IN", "NOT IN", "BETWEEN", "AND", "OR",
            "LIKE", "ILIKE", "IS NULL", "IS NOT NULL", "COALESCE", "NULLIF", "SUBSTRING",
            "LENGTH", "CHAR_LENGTH", "LOWER", "UPPER", "LTRIM", "RTRIM", "REPLACE", "REVERSE",
            "CONCAT", "ABS", "AVG", "CEILING", "COUNT", "FLOOR", "MAX", "MIN", "ROUND", "SUM",
            "AGE", "EXTRACT", "DATE_PART", "NOW", "CURRENT_DATE", "CURRENT_TIME", "CURRENT_TIMESTAMP",
            "DAY", "MONTH", "YEAR", "HOUR", "MINUTE", "SECOND", "TO_CHAR", "TO_DATE",
            "TO_TIMESTAMP", "CAST", "CONVERT", "STRING_AGG", "ARRAY_AGG", "EXISTS", "NOT EXISTS",
            "ANY", "ALL", "SOME", "REGEXP_MATCHES", "RANDOM", "TRUNC", "GREATEST", "LEAST",
            "ROW_NUMBER", "RANK", "DENSE_RANK", "NTILE", "PARTITION BY", "OVER"
        };

        public static readonly string[] SqliteKeywords = new string[]
        {
            "SELECT", "FROM", "WHERE", "JOIN", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN",
            "FULL JOIN", "ON", "GROUP BY", "ORDER BY", "HAVING", "DISTINCT", "UNION",
            "UNION ALL", "LIMIT", "OFFSET", "AS", "IN", "NOT IN", "BETWEEN", "AND", "OR",
            "LIKE", "IS NULL", "IS NOT NULL", "COALESCE", "NULLIF", "SUBSTR", "LENGTH",
            "CHAR", "LOWER", "UPPER", "LTRIM", "RTRIM", "REPLACE", "CONCAT", "ABS", "AVG",
            "COUNT", "FLOOR", "MAX", "MIN", "ROUND", "SUM", "DATE", "TIME", "DATETIME",
            "CURRENT_DATE", "CURRENT_TIME", "CURRENT_TIMESTAMP", "CAST", "IFNULL", "EXISTS",
            "NOT EXISTS", "ANY", "ALL", "SOME", "RANDOM", "GREATEST", "LEAST", "TOTAL"
        };
    }
}
