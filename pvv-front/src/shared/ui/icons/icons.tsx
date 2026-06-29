import type { SVGProps } from 'react';

/**
 * Icon library — small, dependency-free inline SVGs.
 *
 * Stroke icons inherit `currentColor`, so color is controlled by the parent's
 * CSS `color`. Size is controlled by width/height (defaulting to 1em) and can
 * be overridden via props or CSS (`svg { width: ... }`).
 */

type IconProps = SVGProps<SVGSVGElement>;

const strokeBase: IconProps = {
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 2,
  strokeLinecap: 'round',
  strokeLinejoin: 'round',
};

export function CarIcon(props: IconProps) {
  return (
    <svg viewBox="0 0 24 24" {...strokeBase} {...props}>
      <path d="M3 13l1.8-4.6A2 2 0 0 1 6.7 7h10.6a2 2 0 0 1 1.9 1.4L21 13M3 13h18M4 13v3.5h2.2M20 13v3.5h-2.2" />
      <circle cx="7.2" cy="16.5" r="1.6" />
      <circle cx="16.8" cy="16.5" r="1.6" />
    </svg>
  );
}

export function ShieldCheckIcon(props: IconProps) {
  return (
    <svg viewBox="0 0 24 24" {...strokeBase} {...props}>
      <path d="M12 3l7 3v5c0 4.5-3 7.8-7 9-4-1.2-7-4.5-7-9V6l7-3z" />
      <polyline points="9 12 11.3 14.3 15.3 9.8" />
    </svg>
  );
}

export function ShieldIcon(props: IconProps) {
  return (
    <svg viewBox="0 0 24 24" {...strokeBase} {...props}>
      <path d="M12 3l7 3v5c0 4.5-3 7.8-7 9-4-1.2-7-4.5-7-9V6l7-3z" />
    </svg>
  );
}

export function CreditCardIcon(props: IconProps) {
  return (
    <svg viewBox="0 0 24 24" {...strokeBase} {...props}>
      <rect x="3" y="6" width="18" height="12" rx="2" />
      <path d="M3 10h18" />
      <rect x="6" y="13.2" width="4" height="2.4" rx="0.6" />
    </svg>
  );
}

export function CheckIcon(props: IconProps) {
  return (
    <svg viewBox="0 0 24 24" {...strokeBase} strokeWidth={3} {...props}>
      <polyline points="4 12 9 17 20 6" />
    </svg>
  );
}

export function LockIcon(props: IconProps) {
  return (
    <svg viewBox="0 0 24 24" {...strokeBase} {...props}>
      <rect x="5" y="11" width="14" height="9" rx="2" />
      <path d="M8 11V8a4 4 0 0 1 8 0v3" />
    </svg>
  );
}

export function UserIcon(props: IconProps) {
  return (
    <svg viewBox="0 0 24 24" {...strokeBase} {...props}>
      <circle cx="12" cy="8" r="3.4" />
      <path d="M5.5 20a6.5 6.5 0 0 1 13 0" />
    </svg>
  );
}

export function MailIcon(props: IconProps) {
  return (
    <svg viewBox="0 0 24 24" {...strokeBase} {...props}>
      <rect x="3" y="5" width="18" height="14" rx="2.5" />
      <path d="M4 7l8 6 8-6" />
    </svg>
  );
}

export function MobileIcon(props: IconProps) {
  return (
    <svg viewBox="0 0 24 24" {...strokeBase} {...props}>
      <rect x="7" y="2.5" width="10" height="19" rx="2.5" />
      <line x1="11" y1="18.5" x2="13" y2="18.5" />
    </svg>
  );
}

/** Outlined lightning bolt (feature highlight). */
export function BoltIcon(props: IconProps) {
  return (
    <svg viewBox="0 0 24 24" {...strokeBase} {...props}>
      <polygon points="13 2 4 14 11 14 10 22 19 9 12 9 13 2" />
    </svg>
  );
}

/** Filled lightning bolt (compact status chip). */
export function BoltFilledIcon(props: IconProps) {
  return (
    <svg viewBox="0 0 24 24" fill="currentColor" {...props}>
      <path d="M13 2L4.5 13H11l-1 9 9-11.5H13l0-8.5z" />
    </svg>
  );
}

export function DownloadIcon(props: IconProps) {
  return (
    <svg viewBox="0 0 24 24" {...strokeBase} {...props}>
      <path d="M12 3v11m0 0l-4-4m4 4l4-4M5 19h14" />
    </svg>
  );
}

/**
 * Stylized Mercado Pago "handshake/smile" mark.
 * Generic representation for a demo — not the exact trademarked logo.
 * `swooshColor` controls the inner curve color so it works on both the blue
 * circle (white swoosh) and the gradient button (primary swoosh).
 */
export function MercadoPagoMark({
  swooshColor = '#009ee3',
  ...props
}: IconProps & { swooshColor?: string }) {
  return (
    <svg viewBox="0 0 32 32" fill="none" {...props}>
      <ellipse cx="16" cy="16" rx="13" ry="9" fill="#fff" />
      <path
        d="M9 17.5c2 1.8 4.5 1.8 6.5 .6 1.4-.8 2.4-.7 3.4.1.9.7 2 .5 2.6-.4"
        stroke={swooshColor}
        strokeWidth="1.8"
        strokeLinecap="round"
      />
      <path
        d="M9.5 14c1.2 1 2.6 1.1 3.8.5"
        stroke={swooshColor}
        strokeWidth="1.8"
        strokeLinecap="round"
      />
    </svg>
  );
}
