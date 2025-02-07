import React from 'react';
import {
  Modal,
  Paper,
  Box,
  Typography,
  IconButton
} from '@mui/material';
import { Close as CloseIcon } from '@mui/icons-material';
import ReactMarkdown from 'react-markdown';

function AnalysisModal({ open, onClose, analysisResult }) {
  return (
    <Modal
      open={open}
      onClose={onClose}
      aria-labelledby="analysis-modal"
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
          <Typography variant="h6">Quick Insight Analysis</Typography>
          <IconButton onClick={onClose} size="small">
            <CloseIcon />
          </IconButton>
        </Box>

        {analysisResult && (
          <ReactMarkdown>
            {analysisResult}
          </ReactMarkdown>
        )}
      </Paper>
    </Modal>
  );
}

export default AnalysisModal;