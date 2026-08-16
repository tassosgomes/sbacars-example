import { createRoot } from 'react-dom/client';
import { App } from '@app/App';
import { initTelemetry } from './telemetry';
import '@sbacars/ui/tokens.css';
import './index.css';

// Before React, not after: a trace should cover as much of the page's life as possible, and
// initTelemetry is a synchronous, side-effect-only no-op when no collector is configured (A8).
initTelemetry();

// StrictMode is disabled: React 18 dev remount runs the OIDC callback twice,
// reusing the auth code and surfacing as a CORS "Failed to fetch" on /token.
createRoot(document.getElementById('root')!).render(<App />);
