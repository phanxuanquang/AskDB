import React from 'react';
import {
  Modal,
  Paper,
  Box,
  Typography,
  IconButton,
  Button
} from '@mui/material';
import {
  Info as InfoIcon,
  Close as CloseIcon
} from '@mui/icons-material';
import ReactMarkdown from 'react-markdown';

function SqlModal({
  open,
  onClose,
  queryResponse,
  showExplanation,
  onToggleExplanation,
  theme
}) {
  return (
    <Modal
      open={open}
      onClose={onClose}
      aria-labelledby="sql-modal"
    >
      <Paper sx={{
        position: 'absolute',
        top: '50%',
        left: '50%',
        transform: 'translate(-50%, -50%)',
        width: { xs: '90%', sm: 600 },
        maxHeight: '90vh',
        p: 3,
        overflowY: 'auto'
      }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
          <Typography variant="h6">Generated SQL Query</Typography>
          <IconButton onClick={onClose} size="small">
            <CloseIcon />
          </IconButton>
        </Box>

        <pre style={{ 
          whiteSpace: 'pre-wrap',
          wordBreak: 'break-word',
          backgroundColor: theme.palette.grey[100],
          padding: theme.spacing(2),
          borderRadius: theme.shape.borderRadius
        }}>
          {queryResponse?.ConvertedSqlQuery}
        </pre>

        <Button
          startIcon={<InfoIcon />}
          onClick={onToggleExplanation}
          sx={{ mt: 2 }}
        >
          {showExplanation ? 'Hide' : 'View'} Explanation
        </Button>

        {showExplanation && queryResponse?.SqlExplanation && (
          <Box sx={{ mt: 2 }}>
            <ReactMarkdown>
              {queryResponse.SqlExplanation}
            </ReactMarkdown>
          </Box>
        )}
      </Paper>
    </Modal>
  );
}

export default SqlModal;