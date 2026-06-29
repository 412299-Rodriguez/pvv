export type {
  FaqItem,
  UiConfig,
  ProductItem,
  ProductConfig,
  PricingRule,
  PricingConfig,
} from './model/types'
export {
  CONFIG_TYPE,
  emptyUiConfig,
  emptyProductConfig,
  emptyPricingConfig,
  VEHICLE_TYPES,
  VEHICLE_TYPE_LABELS,
  UI_TEXT_FIELDS,
} from './model/types'
export { getConfig, putConfig } from './api/configApi'
