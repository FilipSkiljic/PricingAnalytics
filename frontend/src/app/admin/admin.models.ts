/** One catalog rule, with how many market entries it currently claims. */
export interface CatalogEntry {
  id: number;
  slug: string;
  displayName: string;
  requiredPhrase: string;
  excludedWords: string;
  matchedEntries: number;
}

/** A corpus name no rule currently claims. */
export interface UnmatchedName {
  name: string;
  occurrences: number;
}

/** What a proposed rule would do to the corpus, before it is saved. */
export interface RulePreview {
  wouldMatchTotal: number;
  /** Entries nothing claims today - the actual gain in coverage. */
  currentlyUnmatched: number;
  /** Entries the rule being edited already claims - kept, not gained. */
  alreadyOwned: number;
  /** Entries a different rule claims today; non-zero means this rule would take them over. */
  takenFromOtherServices: number;
  sampleNames: string[];
  affectedServices: string[];
}

/** Why one rule did or did not claim a name. */
export interface CandidateExplanation {
  slug: string;
  displayName: string;
  missingTokens: string[];
  blockingTokens: string[];
  isCandidate: boolean;
}

/** Full trace of a match attempt. */
export interface MatchExplanation {
  rawName: string;
  normalizedTokens: string[];
  winner: CandidateExplanation | null;
  allCandidates: CandidateExplanation[];
}

export interface SystemStatistics {
  registeredBusinesses: number;
  corpusEntries: number;
  catalogServices: number;
  matchedEntries: number;
  coveragePercent: number;
}

export interface BusinessSummary {
  id: number;
  name: string;
  email: string;
  role: string;
  serviceCount: number;
}

export interface CorpusImportResult {
  imported: number;
}
