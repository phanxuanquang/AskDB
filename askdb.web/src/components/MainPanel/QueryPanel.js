import React, { useState } from "react";
import { Box } from "@mui/material";
import { useTheme, useMediaQuery } from "@mui/material";
import QueryInput from "./components/QueryInput";
import ActionButtons from "./components/ActionButtons";
import QueryResults from "./components/QueryResults";
import SqlModal from "./components/SqlModal";
import AnalysisModal from "./components/AnalysisModal";

function QueryPanel() {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down("sm"));

  // States
  const [query, setQuery] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [queryResponse, setQueryResponse] = useState(null);
  const [showSqlModal, setShowSqlModal] = useState(false);
  const [showExplanation, setShowExplanation] = useState(false);
  const [analysisLoading, setAnalysisLoading] = useState(false);
  const [analysisResult, setAnalysisResult] = useState(null);
  const [showAnalysisModal, setShowAnalysisModal] = useState(false);

  // Execute query
  const handleExecuteQuery = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await fetch("/api/query/execute", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ query }),
      });

      if (!response.ok) throw new Error("Query execution failed");

      const data = await response.json();
      setQueryResponse(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  // Quick Insight Analysis
  const handleQuickInsight = async () => {
    setAnalysisLoading(true);
    try {
      const response = await fetch("/api/query/analyze", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ queryResult: queryResponse.QueryResult }),
      });

      if (!response.ok) throw new Error("Analysis failed");

      const data = await response.json();
      setAnalysisResult(data);
      setShowAnalysisModal(true);
    } catch (err) {
      setError(err.message);
    } finally {
      setAnalysisLoading(false);
    }
  };

  // Export to CSV
  const handleExport = () => {
    if (!queryResponse?.QueryResult) return;

    const headers = Object.keys(queryResponse.QueryResult[0] || {});
    const csvContent = [
      headers.join(","),
      ...queryResponse.QueryResult.map((row) =>
        headers.map((header) => `"${row[header]}"`).join(",")
      ),
    ].join("\n");

    const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
    const link = document.createElement("a");
    link.href = URL.createObjectURL(blob);
    link.download = "query_result.csv";
    link.click();
  };

  return (
    <Box
      sx={{
        p: { xs: 2, sm: 3 },
        background: "white",
        borderRadius: 3,
        width: "50vw"
      }}
    >
      <QueryInput
        query={query}
        setQuery={setQuery}
        loading={loading}
        error={error}
      />

      <ActionButtons
        isMobile={isMobile}
        loading={loading}
        query={query}
        queryResponse={queryResponse}
        analysisLoading={analysisLoading}
        onExecute={handleExecuteQuery}
        onQuickInsight={handleQuickInsight}
        onViewSql={() => setShowSqlModal(true)}
        onExport={handleExport}
      />

      <QueryResults queryResponse={queryResponse} />

      <SqlModal
        open={showSqlModal}
        onClose={() => setShowSqlModal(false)}
        queryResponse={queryResponse}
        showExplanation={showExplanation}
        onToggleExplanation={() => setShowExplanation(!showExplanation)}
        theme={theme}
      />

      <AnalysisModal
        open={showAnalysisModal}
        onClose={() => setShowAnalysisModal(false)}
        analysisResult={analysisResult}
      />
    </Box>
  );
}

export default QueryPanel;
