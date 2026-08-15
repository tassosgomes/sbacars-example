import type { Config } from 'tailwindcss';
import uiPreset from '@sbacars/ui/tailwind.preset';

export default {
  content: [
    './index.html',
    './src/**/*.{js,ts,jsx,tsx}',
    '../../packages/ui/src/**/*.{js,ts,jsx,tsx}',
  ],
  presets: [uiPreset],
  theme: {
    extend: {},
  },
  plugins: [],
} satisfies Config;
