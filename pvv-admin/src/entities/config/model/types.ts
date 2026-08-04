// Config blobs are stored/served PascalCase (exactly as pvv-config persists them);
// the editors read and write these shapes verbatim.

export interface FaqItem {
  Question: string
  Answer: string
}

export interface UiConfig {
  PrimaryColor: string
  SecondaryColor: string
  LogoUrl: string
  CompanyDisplayName: string
  WelcomeText: string
  FooterText: string
  Texts: Record<string, string>
  AdImages: string[]
  TermsText: string
  PrivacyText: string
  Faqs: FaqItem[]
}

export interface ProductItem {
  ProductId: string
  Name: string
  CoverageType: string
  Conditions: string
  IsActive: boolean
}
export interface ProductConfig {
  Products: ProductItem[]
}

export interface PricingRule {
  PricingId: string
  ProductId: string
  VehicleType: string
  YearFrom: number
  YearTo: number
  Price: number
}
export interface PricingConfig {
  Rules: PricingRule[]
}

/**
 * Copy of the abandoned-cart recovery email. Text only — the HTML layout, the branding
 * and the button belong to pvv-bff's renderer. Any field may use the placeholders in
 * RECOVERY_PLACEHOLDERS, and any field left EMPTY falls back to the system default.
 */
export interface RecoveryEmailConfig {
  Subject: string
  Greeting: string
  Intro: string
  ButtonLabel: string
  Closing: string
}

export const CONFIG_TYPE = {
  ui: 'PVV_UI_CONFIG',
  product: 'PRODUCT_CONFIG',
  pricing: 'PRICING_CONFIG',
  recoveryEmail: 'RECOVERY_EMAIL_CONFIG',
} as const

export const emptyRecoveryEmailConfig: RecoveryEmailConfig = {
  Subject: '',
  Greeting: '',
  Intro: '',
  ButtonLabel: '',
  Closing: '',
}

/** What an operator may drop into the copy. Only data the lead itself gave us. */
export const RECOVERY_PLACEHOLDERS: { token: string; label: string }[] = [
  { token: '{nombre}', label: 'Nombre de pila' },
  { token: '{patente}', label: 'Patente' },
  { token: '{vehiculo}', label: 'Vehículo' },
  { token: '{producto}', label: 'Producto cotizado' },
  { token: '{precio}', label: 'Precio cotizado' },
  { token: '{compania}', label: 'Nombre de la compañía' },
]

/**
 * Shown as PLACEHOLDER text so the operator can see what an empty field will send.
 * These mirror pvv-bff's defaults, which are the ones actually used — an empty field
 * is stored empty and resolved there, never copied from here. If the two ever drift,
 * the hint goes stale but nothing sends wrong.
 */
export const RECOVERY_DEFAULT_HINTS: Record<keyof RecoveryEmailConfig, string> = {
  Subject: 'Tu cotización de {compania} sigue disponible',
  Greeting: 'Hola {nombre},',
  Intro:
    'Vimos que empezaste a cotizar el seguro de tu {vehiculo} y no llegaste a completar la compra. Tu cotización sigue disponible y podés retomarla cuando quieras.',
  ButtonLabel: 'Retomar mi compra',
  Closing: 'Si ya contrataste tu seguro por otro medio, ignorá este mensaje.',
}

export const emptyUiConfig: UiConfig = {
  PrimaryColor: '#0071ce',
  SecondaryColor: '#003d7a',
  LogoUrl: '',
  CompanyDisplayName: '',
  WelcomeText: '',
  FooterText: '',
  Texts: {},
  AdImages: [],
  TermsText: '',
  PrivacyText: '',
  Faqs: [],
}

export const emptyProductConfig: ProductConfig = { Products: [] }
export const emptyPricingConfig: PricingConfig = { Rules: [] }

/** Vehicle types pvv-soat understands (for pricing rules) — values stay in English. */
export const VEHICLE_TYPES = ['Car', 'Motorcycle', 'Truck', 'Van'] as const

/** Spanish labels shown to the operator (the stored value stays English). */
export const VEHICLE_TYPE_LABELS: Record<string, string> = {
  Car: 'Auto',
  Motorcycle: 'Moto',
  Truck: 'Camioneta',
  Van: 'Utilitario',
}

/** Known UI text keys the portal reads, with friendly labels for the editor. */
export const UI_TEXT_FIELDS: { key: string; label: string }[] = [
  { key: 'introTitle', label: 'Título principal' },
  { key: 'introSubtitle', label: 'Subtítulo' },
  { key: 'feature1Title', label: 'Beneficio 1 — título' },
  { key: 'feature1Text', label: 'Beneficio 1 — texto' },
  { key: 'feature2Title', label: 'Beneficio 2 — título' },
  { key: 'feature2Text', label: 'Beneficio 2 — texto' },
  { key: 'feature3Title', label: 'Beneficio 3 — título' },
  { key: 'feature3Text', label: 'Beneficio 3 — texto' },
  { key: 'ratingText', label: 'Texto de reputación' },
  { key: 'plateTitle', label: 'Paso patente — título' },
  { key: 'plateSubtitle', label: 'Paso patente — subtítulo' },
  { key: 'plateCta', label: 'Botón cotizar' },
  { key: 'secureNote', label: 'Nota de seguridad' },
  { key: 'holderTitle', label: 'Paso titular — título' },
  { key: 'holderSubtitle', label: 'Paso titular — subtítulo' },
  { key: 'coverageTitle', label: 'Paso cobertura — título' },
]
