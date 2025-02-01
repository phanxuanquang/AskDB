import React from 'react';
import { Box, CircularProgress, Typography, Fade } from '@mui/material';

function LoadingScreen() {
  return (
    <Fade in={true}>
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          height: '100vh',
        }}
      >
        <CircularProgress size={100} thickness={3} sx={{color: "primary"}}/>
        <Typography 
          variant="h6" 
          mt={2}
          color="primary"
          fontWeight="medium"
        >
          Getting started...
        </Typography>
      </Box>
    </Fade>
  );
}

export default LoadingScreen;
