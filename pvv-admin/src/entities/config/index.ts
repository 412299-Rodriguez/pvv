export type {
  FaqItem,
  UiConfig,
  ProductItem,
  ProductConfig,
  PricingRule,
  PricingConfig,
  RecoveryEmailConfig,
} from './model/types'
export {
  CONFIG_TYPE,
  emptyUiConfig,
  emptyProductConfig,
  emptyPricingConfig,
  emptyRecoveryEmailConfig,
  RECOVERY_PLACEHOLDERS,
  RECOVERY_DEFAULT_HINTS,
  VEHICLE_TYPES,
  VEHICLE_TYPE_LABELS,
  UI_TEXT_FIELDS,
} from './model/types'
export { getConfig, putConfig } from './api/configApi'
