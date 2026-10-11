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

export interface AnswerOptionResponse {
  id: number;
  text: string;
  isCorrect: boolean;
}

export interface QuestionResponse {
  id: number;
  text: string;
  timeLimitSeconds: number;
  points: number;
  orderIndex: number;
  answerOptions: AnswerOptionResponse[];
}

export interface QuizSummary {
  id: number;
  title: string;
  description: string | null;
}

export interface QuizEditorResponse {
  id: number;
  title: string;
  description: string | null;
  questions: QuestionResponse[];
}

export interface AnswerOptionRequest {
  text: string;
  isCorrect: boolean;
}

export interface QuestionRequest {
  text: string;
  timeLimitSeconds: number;
  points: number;
  orderIndex: number;
  answerOptions: AnswerOptionRequest[];
}

export interface CreateQuizRequest {
  title: string;
  description: string | null;
  questions: QuestionRequest[];
}

export interface UpdateQuizRequest {
  title: string;
  description: string | null;
  questions: QuestionRequest[];
}
