export interface SetSummary {
  id: number;
  name: string;
  cardCount: number;
  createdAt: string;
}

export interface CardResponse {
  id: number;
  term: string;
  definition: string;
}
