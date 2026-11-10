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
  isPublic: boolean;
  creatorName: string;
}

export interface QuizEditorResponse {
  id: number;
  title: string;
  description: string | null;
  questions: QuestionResponse[];
  isPublic: boolean;
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

export interface PracticeQuizSummary {
  id: number;
  title: string;
  description: string | null;
  categoryId: number | null;
  languageId: number | null;
  difficulty: number | null;
  questionCount: number;
}

export interface PracticeFilters {
  category?: number;
  language?: number;
  difficulty?: number;
}

export interface PracticeAnswerOptionResponse {
  id: number;
  text: string;
}

export interface PracticeQuestionResponse {
  id: number;
  text: string;
  orderIndex: number;
  answerOptions: PracticeAnswerOptionResponse[];
}

export interface PracticeQuizResponse {
  id: number;
  title: string;
  description: string | null;
  questions: PracticeQuestionResponse[];
}

export interface PracticeAnswerRequest {
  quizId: number;
  questionId: number;
  answerOptionId: number;
}

export interface PracticeAnswerResponse {
  isCorrect: boolean;
  correctAnswerOptionId: number;
}
