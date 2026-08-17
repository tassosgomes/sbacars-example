import type { Config } from 'tailwindcss';

const preset: Partial<Config> = {
  theme: {
    extend: {
      colors: {
        primary: {
          50: 'var(--color-primary-50)',
          100: 'var(--color-primary-100)',
          500: 'var(--color-primary-500)',
          600: 'var(--color-primary-600)',
          700: 'var(--color-primary-700)',
          DEFAULT: 'var(--color-primary-500)',
          foreground: 'var(--color-primary-foreground)',
        },
        accent: {
          500: 'var(--color-accent-500)',
          600: 'var(--color-accent-600)',
          DEFAULT: 'var(--color-accent-500)',
          foreground: 'var(--color-accent-foreground)',
        },
        action: {
          DEFAULT: 'var(--color-action-orange)',
          hover: 'var(--color-action-orange-hover)',
        },
        trust: {
          DEFAULT: 'var(--color-trust-green)',
          light: 'var(--color-trust-green-light)',
        },
        neutral: {
          50: 'var(--color-neutral-50)',
          100: 'var(--color-neutral-100)',
          200: 'var(--color-neutral-200)',
          300: 'var(--color-neutral-300)',
          400: 'var(--color-neutral-400)',
          500: 'var(--color-neutral-500)',
          600: 'var(--color-neutral-600)',
          700: 'var(--color-neutral-700)',
          800: 'var(--color-neutral-800)',
          900: 'var(--color-neutral-900)',
          foreground: 'var(--color-neutral-foreground)',
        },
        background: 'var(--color-background)',
        surface: {
          DEFAULT: 'var(--color-surface)',
          container: 'var(--color-surface-container)',
        },
        border: 'var(--color-border)',
        muted: 'var(--color-muted)',
        danger: {
          DEFAULT: 'var(--color-danger-500)',
          600: 'var(--color-danger-600)',
          foreground: 'var(--color-danger-foreground)',
        },
      },
      fontFamily: {
        sans: ['var(--font-family-sans)'],
      },
      fontSize: {
        xs: ['var(--font-size-xs)', { lineHeight: 'var(--line-height-normal)' }],
        sm: ['var(--font-size-sm)', { lineHeight: 'var(--line-height-normal)' }],
        base: ['var(--font-size-base)', { lineHeight: 'var(--line-height-normal)' }],
        lg: ['var(--font-size-lg)', { lineHeight: 'var(--line-height-normal)' }],
        xl: ['var(--font-size-xl)', { lineHeight: 'var(--line-height-tight)' }],
        '2xl': ['var(--font-size-2xl)', { lineHeight: 'var(--line-height-tight)' }],
        '3xl': ['var(--font-size-3xl)', { lineHeight: 'var(--line-height-tight)' }],
      },
      spacing: {
        0: 'var(--spacing-0)',
        1: 'var(--spacing-1)',
        2: 'var(--spacing-2)',
        3: 'var(--spacing-3)',
        4: 'var(--spacing-4)',
        5: 'var(--spacing-5)',
        6: 'var(--spacing-6)',
        8: 'var(--spacing-8)',
        10: 'var(--spacing-10)',
        12: 'var(--spacing-12)',
        16: 'var(--spacing-16)',
      },
      borderRadius: {
        sm: 'var(--radius-sm)',
        md: 'var(--radius-md)',
        lg: 'var(--radius-lg)',
        xl: 'var(--radius-xl)',
        full: 'var(--radius-full)',
      },
      boxShadow: {
        sm: 'var(--shadow-sm)',
        md: 'var(--shadow-md)',
        lg: 'var(--shadow-lg)',
      },
    },
  },
};

export default preset;
