/** An insurance product the user can purchase. */
export interface Coverage {
  id: string;
  /** Commercial name, e.g. "Cobertura Total". */
  name: string;
  /** Coverage class, e.g. "Todo Riesgo". */
  coverageType: string;
  /** Annual price in ARS. */
  pricePerYear: number;
  /** Marks the highlighted / "most chosen" option. */
  recommended: boolean;
  /** Short selling points shown as a checklist. */
  benefits: string[];
}
