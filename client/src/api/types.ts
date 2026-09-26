export interface SetSummary {
  id: number;
  name: string;
  cardCount: number;
  createdAt: Date;
}

export interface CardResponse {
  id: number;
  term: string;
  definition: string;
}
