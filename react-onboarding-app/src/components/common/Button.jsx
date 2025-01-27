import React from 'react';
import { Button as MuiButton } from '@mui/material';

const Button = ({ variant = 'contained', color = 'primary', children, ...props }) => {
  return (
    <MuiButton variant={variant} color={color} {...props}>
      {children}
    </MuiButton>
  );
};

export default Button;