import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from '@app/App';
import { initTelemetry } from './telemetry';
import '@sbacars/ui/tokens.css';
import './index.css';

// Before React, not after: a trace should cover as much of the page's life as possible, and
// initTelemetry is a synchronous, side-effect-only no-op when no collector is configured (A8).
initTelemetry();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
