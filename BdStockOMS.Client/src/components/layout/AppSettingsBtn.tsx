import React from 'react';
import { TopbarIconBtn } from './TopbarIconBtn';

export const AppSettingsBtn: React.FC = () => {
  return (
    <TopbarIconBtn 
      icon="⚙️" 
      title="App Settings" 
      onClick={() => console.log('Settings clicked')} 
    />
  );
};
