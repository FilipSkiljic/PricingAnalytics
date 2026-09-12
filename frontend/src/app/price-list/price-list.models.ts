import { PriceRecommendation } from '../analysis/analysis.models';

/** One row of GET /my/services: a price-list entry with its market verdict. */
export interface PriceListItem {
  id: number;
  name: string;
  price: number;
  /** Null when the service name matched no canonical service in the catalog. */
  recommendation: PriceRecommendation | null;
}

/** Response shape of POST /my/services/import. */
export interface ImportResult {
  imported: number;
  skipped: number;
}
