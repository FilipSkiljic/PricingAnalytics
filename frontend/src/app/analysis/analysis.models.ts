/** Mirrors the API's PriceVerdict enum (serialized as a number). */
export enum PriceVerdict {
  TooLow = 0,
  Fair = 1,
  TooHigh = 2,
}

export interface HistogramBin {
  lowerBound: number;
  upperBound: number;
  count: number;
}

/** Response shape of GET /analyze. */
export interface PriceRecommendation {
  canonicalServiceId: string;
  canonicalServiceName: string;
  /** Entries used for the statistics, after outlier removal. */
  sampleSize: number;
  sampleSizeBeforeOutlierRemoval: number;
  median: number;
  q1: number;
  q3: number;
  userPrice: number;
  /** Position of the user's price within the market, 0-100. */
  percentileRank: number;
  verdict: PriceVerdict;
  distribution: HistogramBin[];
}

/** Response shape of GET /services. */
export interface AvailableService {
  id: string;
  displayName: string;
}
