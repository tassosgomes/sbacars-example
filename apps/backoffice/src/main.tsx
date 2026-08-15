import { createRoot } from 'react-dom/client';
import { App } from '@app/App';
import '@sbacars/ui/tokens.css';
import './index.css';

// StrictMode is disabled: React 18 dev remount runs the OIDC callback twice,
// reusing the auth code and surfacing as a CORS "Failed to fetch" on /token.
createRoot(document.getElementById('root')!).render(<App />);
