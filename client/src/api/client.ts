import axios from "axios";
import { clearSession, getToken } from "./auth";
import {AnswerOptionRequest, AnswerOptionResponse, CardResponse, CreateQuizRequest, QuestionRequest, QuestionResponse, QuizEditorResponse, QuizSummary, SetSummary, UpdateQuizRequest,} from "./types";

const baseURL = import.meta.env.VITE_API_BASE_URL ?? "/api";

const client = axios.create({
  baseURL,
  headers: { "Content-Type": "application/json" },
});

client.interceptors.request.use((config) => {
  const token = getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});
client.interceptors.response.use(
  (response) => response,
  (error) => {
    const isAuthEndpoint =
      error.config?.url?.includes("/auth/login") ||
      error.config?.url?.includes("/auth/register");

    if (error.response?.status === 401 && !isAuthEndpoint) {
      clearSession();
      window.location.href = "/login";
    }
    return Promise.reject(error);
  },
);

export interface AuthResponse {
  token: string;
  email: string;
  role: string;
}

export default client;

export const authApi = {
  register: (email: string, password: string) =>
    client.post<{ message: string }>("/auth/register", { email, password }),
  login: (email: string, password: string) =>
    client.post<AuthResponse>("/auth/login", { email, password }),
};

export const setApi = {
  getAll: () => client.get<SetSummary[]>("/sets"),
  getCards: (setId: number) =>
    client.get<CardResponse[]>(`/sets/${setId}/cards`),
  create: (name: string) => client.post<SetSummary>("/sets", { name }),
};

export const cardApi = {
  add: (setId: number, term: string, definition: string) =>
    client.post<CardResponse>(`/sets/${setId}/cards`, { term, definition }),
};

export const quizApi = {
  getMine: () => client.get<QuizSummary[]>("/quizzes/mine"),
  getById: (quizId: number) => client.get<QuizEditorResponse>(`/quizzes/${quizId}`),
  create: (request: CreateQuizRequest) => client.post<QuizEditorResponse>("/quizzes", request),
  update: (quizId: number, request: UpdateQuizRequest) => client.put<QuizEditorResponse>(`/quizzes/${quizId}`, request),
  delete: (quizId: number) => client.delete(`/quizzes/${quizId}`),

  addQuestion: (quizId: number, request: QuestionRequest) => client.post<QuestionResponse>(`/quizzes/${quizId}/questions`, request),
  updateQuestion: (questionId: number, request: QuestionRequest) => client.put<QuestionResponse>(`/quizzes/questions/${questionId}`, request),
  deleteQuestion: (questionId: number) => client.delete(`/quizzes/questions/${questionId}`),

  addAnswerOption: (questionId: number, request: AnswerOptionRequest) => client.post<AnswerOptionResponse>(`/quizzes/questions/${questionId}/answers`, request),
  updateAnswerOption: (answerOptionId: number, request: AnswerOptionRequest) => client.put<AnswerOptionResponse>(`/quizzes/answers/${answerOptionId}`, request),
  deleteAnswerOption: (answerOptionId: number) => client.delete(`/quizzes/answers/${answerOptionId}`),
};