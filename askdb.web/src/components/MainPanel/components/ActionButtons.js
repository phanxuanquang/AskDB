import React from 'react';
import {
  Box,
  Button,
  CircularProgress,
  Tooltip
} from '@mui/material';
import {
  Insights as AnalyticsIcon,
  Code as CodeIcon,
  FileDownload as FileDownloadIcon,
} from '@mui/icons-material';

function ActionButtons({
  isMobile,
  loading,
  query,
  queryResponse,
  analysisLoading,
  onExecute,
  onQuickInsight,
  onViewSql,
  onExport
}) {
  return (
    <Box sx={{ 
      display: 'flex', 
      flexDirection: { xs: 'column', sm: 'row' },
      gap: 1,
      mb: 3
    }}>
      <Button
        variant="contained"
        onClick={onExecute}
        disabled={!query.trim() || loading}
        fullWidth={isMobile}
        disableElevation
        sx={{ flex: 2 }}
      >
        {loading ? (
          <CircularProgress size={24} color="inherit" />
        ) : (
          'Execute'
        )}
      </Button>

      <Box sx={{ 
        display: 'flex',
        gap: 1,
        flex: 1,
        flexDirection: { xs: 'row', sm: 'row' }
      }}>
        <Tooltip title="Quick Insight">
          <span>
            <Button
              startIcon={<AnalyticsIcon />}
              onClick={onQuickInsight}
              disabled={!queryResponse || analysisLoading}
              fullWidth
              variant="outlined"
            >
              {analysisLoading ? <CircularProgress size={24} /> : 'Insight'}
            </Button>
          </span>
        </Tooltip>

        <Tooltip title="View SQL">
          <span>
            <Button
              startIcon={<CodeIcon />}
              onClick={onViewSql}
              disabled={!queryResponse}
              fullWidth
              variant="outlined"
            >
              SQL
            </Button>
          </span>
        </Tooltip>

        <Tooltip title="Export Results">
          <span>
            <Button
              startIcon={<FileDownloadIcon />}
              onClick={onExport}
              disabled={!queryResponse?.QueryResult}
              fullWidth
              variant="outlined"
            >
              Export
            </Button>
          </span>
        </Tooltip>
      </Box>
    </Box>
  );
}

export default ActionButtons;
