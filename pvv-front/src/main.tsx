import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';

// Global styles must load before any component styles so CSS-module rules win.
import '@/app/styles/tokens.css';
import '@/app/styles/global.css';

import { App } from '@/app/App';

const rootEl = document.getElementById('root');
if (!rootEl) throw new Error('Root element #root not found');

createRoot(rootEl).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
