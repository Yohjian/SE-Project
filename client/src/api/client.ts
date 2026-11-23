import axios from "axios";

import { clearSession, getToken } from "./auth";
import type {
    CardResponse,
    CreateQuizRequest,
    PracticeAnswerRequest,
    PracticeAnswerResponse,
    PracticeFiltersResponse,
    PracticeQuizResponse,
    QuizEditorResponse,
    QuizSummary,
    SetFolder,
    SetSummary,
    UpdateQuizRequest,
} from "./types";

export interface AuthResponse {
    token: string;
    email: string;
    role: string;
}

const client = axios.create({
    baseURL: import.meta.env.VITE_API_BASE_URL ?? "/api",
    headers: { "Content-Type": "application/json" },
});

client.interceptors.request.use((config) => {
    const token = getToken();
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
});

client.interceptors.response.use(
    (response) => response,
    (error) => {
        if (
            axios.isAxiosError(error) &&
            error.response?.status === 401 &&
            error.config?.url !== "/auth/login" &&
            error.config?.url !== "/auth/register" &&
            getToken()
        ) {
            clearSession();
            window.location.href = "/login";
        }
        return Promise.reject(error);
    },
);

export default client;

export const authApi = {
    register: (email: string, password: string) =>
        client.post<AuthResponse>("/auth/register", { email, password }),

    login: (email: string, password: string) =>
        client.post<AuthResponse>("/auth/login", { email, password }),
};

export const setFolderApi = {
    getAll: () => client.get<SetFolder[]>("/set-folders"),

    create: (name: string) => client.post<SetFolder>("/set-folders", { name }),

    rename: (folderId: number, name: string) =>
        client.put<SetFolder>(`/set-folders/${folderId}`, { name }),

    delete: (folderId: number) => client.delete(`/set-folders/${folderId}`),
};

export const setApi = {
    getAll: () => client.get<SetSummary[]>("/sets"),

    getCards: (setId: number) =>
        client.get<CardResponse[]>(`/sets/${setId}/cards`),

    create: (name: string, folderId: number | null) =>
        client.post<SetSummary>("/sets", { name, folderId }),

    delete: (setId: number) => client.delete(`/sets/${setId}`),

    moveToFolder: (setId: number, folderId: number | null) =>
        client.put(`/sets/${setId}/folder`, { folderId }),

    download: (setId: number, format: "json" | "csv") =>
        client.get<Blob>(`/sets/${setId}/download/${format}`, {
            responseType: "blob",
        }),
};

export const cardApi = {
    add: (
        setId: number,
        term: string,
        definition: string,
        metadata?: Record<string, unknown>,
    ) =>
        client.post<CardResponse>(`/sets/${setId}/cards`, {
            term,
            definition,
            metadata,
        }),

    delete: (setId: number, cardId: number) =>
        client.delete(`/sets/${setId}/cards/${cardId}`),
};

export const quizApi = {
    getMine: () => client.get<QuizSummary[]>("/quizzes/mine"),

    getById: (quizId: number) =>
        client.get<QuizEditorResponse>(`/quizzes/${quizId}`),

    create: (request: CreateQuizRequest) =>
        client.post<QuizSummary>("/quizzes", request),

    update: (quizId: number, request: UpdateQuizRequest) =>
        client.put(`/quizzes/${quizId}`, request),

    delete: (quizId: number) => client.delete(`/quizzes/${quizId}`),

    addQuestion: (quizId: number, request: unknown) =>
        client.post(`/quizzes/${quizId}/questions`, request),

    updateQuestion: (questionId: number, request: unknown) =>
        client.put(`/quizzes/questions/${questionId}`, request),

    deleteQuestion: (questionId: number) =>
        client.delete(`/quizzes/questions/${questionId}`),

    addAnswerOption: (questionId: number, request: unknown) =>
        client.post(`/quizzes/questions/${questionId}/answers`, request),

    updateAnswerOption: (answerOptionId: number, request: unknown) =>
        client.put(`/quizzes/answers/${answerOptionId}`, request),

    deleteAnswerOption: (answerOptionId: number) =>
        client.delete(`/quizzes/answers/${answerOptionId}`),

    updateVisibility: (quizId: number, isPublic: boolean) =>
        client.patch(`/quizzes/${quizId}/visibility`, { isPublic }),

    copy: (quizId: number) =>
        client.post<QuizSummary>(`/quizzes/${quizId}/copy`),

    getPublic: () => client.get<QuizSummary[]>("/quizzes/public"),

    getPublicById: (quizId: number) =>
        client.get<QuizEditorResponse>(`/quizzes/public/${quizId}`),
};

export const practiceApi = {
    getFilters: () => client.get<PracticeFiltersResponse>("/practice/filters"),

    getQuizzes: (filters?: {
        category?: number;
        language?: number;
        difficulty?: number;
    }) => client.get<unknown>("/practice/quizzes", { params: filters }),

    getQuiz: (quizId: number) =>
        client.get<PracticeQuizResponse>(`/practice/quizzes/${quizId}`),

    checkAnswer: (request: PracticeAnswerRequest) =>
        client.post<PracticeAnswerResponse>("/practice/answer", request),
};
