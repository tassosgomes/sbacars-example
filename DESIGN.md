---
name: Autentico Brazil
colors:
  surface: '#f9f9ff'
  surface-dim: '#d9d9e1'
  surface-bright: '#f9f9ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f3f3fb'
  surface-container: '#ededf5'
  surface-container-high: '#e7e7ef'
  surface-container-highest: '#e2e2ea'
  on-surface: '#191b21'
  on-surface-variant: '#47464c'
  inverse-surface: '#2e3036'
  inverse-on-surface: '#f0f0f8'
  outline: '#78767c'
  outline-variant: '#c8c5cc'
  surface-tint: '#5e5d6b'
  primary: '#191925'
  on-primary: '#ffffff'
  primary-container: '#2e2e3a'
  on-primary-container: '#9795a4'
  inverse-primary: '#c7c5d4'
  secondary: '#006d37'
  on-secondary: '#ffffff'
  secondary-container: '#8ef9ab'
  on-secondary-container: '#00743b'
  tertiary: '#301200'
  on-tertiary: '#ffffff'
  tertiary-container: '#4f2300'
  on-tertiary-container: '#eb7711'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#e3e1f1'
  primary-fixed-dim: '#c7c5d4'
  on-primary-fixed: '#1a1b26'
  on-primary-fixed-variant: '#464652'
  secondary-fixed: '#8ef9ab'
  secondary-fixed-dim: '#72dc91'
  on-secondary-fixed: '#00210c'
  on-secondary-fixed-variant: '#005228'
  tertiary-fixed: '#ffdbc7'
  tertiary-fixed-dim: '#ffb688'
  on-tertiary-fixed: '#311300'
  on-tertiary-fixed-variant: '#733600'
  background: '#f9f9ff'
  on-background: '#191b21'
  surface-variant: '#e2e2ea'
  deep-navy: '#2E2E3A'
  trust-green: '#018444'
  action-orange: '#FC8422'
  border-muted: '#CDCDDB'
  text-main: '#2E2E3A'
  text-muted: '#4A4A4A'
typography:
  headline-xl:
    fontFamily: Inter
    fontSize: 48px
    fontWeight: '700'
    lineHeight: 56px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Inter
    fontSize: 32px
    fontWeight: '700'
    lineHeight: 40px
    letterSpacing: -0.01em
  headline-lg-mobile:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '700'
    lineHeight: 32px
  headline-md:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
  body-lg:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '400'
    lineHeight: 28px
  body-md:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-sm:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  label-caps:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '700'
    lineHeight: 16px
    letterSpacing: 0.05em
  data-tabular:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '500'
    lineHeight: 20px
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  unit: 8px
  container-max: 1280px
  gutter: 24px
  margin-mobile: 16px
  margin-desktop: 40px
  stack-sm: 8px
  stack-md: 16px
  stack-lg: 32px
---

## Brand & Style

The design system is built for a high-end, trustworthy automotive marketplace catering to the Brazilian market. The brand personality is rooted in **transparency, professional expertise, and curated quality**. Unlike the aggressive sales tactics of traditional dealerships, this system prioritizes an educational approach, guiding users through the complex journey of buying or selling a premium vehicle with calm assurance.

The visual style follows a **Modern Corporate Minimalism** movement. It utilizes high-quality photography as a cornerstone of the interface, supported by generous whitespace that allows vehicle specifications to breathe. The aesthetic is "curated"—every element feels intentional, removing visual noise to focus on the technical details and condition of the cars. The result is a premium, gallery-like experience that feels more like a concierge service than a generic classifieds site.

## Colors

This design system uses a sophisticated palette designed to evoke institutional trust and clarity. 

- **Primary (Deep Navy):** Used for typography, navigation headers, and primary structural elements. It provides a grounded, professional foundation.
- **Secondary (Trust Green):** Inspired by local market leaders but refined. This color signifies "Verified" status, certifications, and "Good Deal" indicators to build confidence.
- **Tertiary (Action Orange):** A vibrant, high-contrast accent reserved exclusively for primary conversion points (Lead capture, "Buy Now", Schedule Inspection).
- **Neutrals:** The background uses a very subtle off-white (#F7F7FF) to reduce screen glare compared to pure white, while pure white is reserved for cards and elevated surfaces to create a crisp, "layered" look.

## Typography

Inter is chosen for its exceptional legibility and systematic feel, which aligns with the "educational" brand pillar. 

- **Scale:** Headlines use a tight tracking (letter-spacing) to feel impactful and modern.
- **Hierarchy:** Use `headline-xl` for hero sections and vehicle names. 
- **Data Display:** For technical specifications and pricing, use the `data-tabular` style which utilizes OpenType features for lining and tabular figures, ensuring that price lists and spec tables align perfectly for easy comparison.
- **Labels:** Small caps are used for metadata (e.g., "KILOMETRAGE", "FUEL TYPE") to create a clear distinction from the data values.

## Layout & Spacing

The layout employs a **Fixed Grid** system on desktop for a controlled, editorial feel, transitioning to a fluid model on mobile devices.

- **Grid:** A 12-column grid is used for desktop (1280px max-width). Vehicle listings and detail pages often utilize an 8-column main content area for technical specs with a 4-column sticky sidebar for lead capture forms.
- **Rhythm:** An 8px base unit governs all spacing.
- **Mobile:** Margins scale down to 16px to maximize real estate for car imagery.
- **Whitespace:** Use "Generous" vertical spacing (`stack-lg`) between logical sections (e.g., between the Image Gallery and the Features List) to prevent information overload.

## Elevation & Depth

To maintain a "High-End" feel, the system avoids heavy shadows. Instead, it uses **Low-Contrast Outlines** and **Tonal Layers**.

- **Cards:** Surface-level cards use a 1px border in `#CDCDDB` with no shadow. On hover, a very subtle, large-radius ambient shadow (10% opacity of Deep Navy) may be applied to indicate interactivity.
- **Lead Forms:** To create a sense of importance and "focus," lead capture forms use a slightly elevated white surface against the `#F7F7FF` background, effectively "floating" the conversion area.
- **Depth:** Background blurs (Glassmorphism) are used sparingly, only for mobile navigation overlays to maintain context of the vehicle images underneath.

## Shapes

The shape language is **Soft and Professional**. 

- **Global Radius:** A 4px (0.25rem) base radius is applied to buttons and input fields to keep them feeling precise and modern.
- **Cards:** Use `rounded-lg` (8px) for vehicle listing cards to provide a friendlier, more approachable frame for the sharp lines of automotive photography.
- **Form Inputs:** Keep corners crisp with the 4px radius to maintain a "serious/official" document feel for lead forms.

## Components

- **Vehicle Cards:** These must include a high-aspect-ratio image, a "Verified" badge using the secondary green, and a clear price hierarchy. Avoid cluttered icons; use clean typography for specs (e.g., "2023 • 15.000 km").
- **Lead Capture Forms:** Use high-contrast input fields with clear labels. The primary CTA button should be the Tertiary Orange, spanning the full width of the form container.
- **Data Tables:** For technical specs, use zebra-striping with the neutral color (#F7F7FF) and `data-tabular` typography. Borders should be horizontal only to maintain a clean vertical flow.
- **Buttons:** 
  - *Primary:* Action Orange with white text for main conversions.
  - *Secondary:* Deep Navy outline with navy text for "View Details" or secondary actions.
  - *Tertiary:* Ghost style (no border/background) for "Compare" or "Save" actions.
- **Badges:** Small, high-contrast pills used to indicate "Automatic," "Single Owner," or "Warranty."