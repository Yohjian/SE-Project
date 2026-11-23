import axios from "axios";
import { clearSession, getToken } from "./auth";
import {
    AnswerOptionRequest,
    AnswerOptionResponse,
    CardResponse,
    CreateQuizRequest,
    QuestionRequest,
    QuestionResponse,
    QuizEditorResponse,
    QuizSummary,
    SetSummary,
    UpdateQuizRequest,
    PracticeQuizSummary,
    PracticeFilters,
    PracticeQuizResponse,
    PracticeAnswerRequest,
    PracticeAnswerResponse,
    PracticeFiltersResponse,
    SetFolder,
} from "./types";

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

        if (error.response?.status === 401 && !isAuthEndpoint && getToken()) {
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

    create: (name: string, folderId: number) =>
        client.post<SetSummary>("/sets", { name, folderId }),
    download: (setId: number, format: "json" | "csv") =>
        client.get<Blob>(`/sets/${setId}/download/${format}`, {
            responseType: "blob",
        }),

    delete: (setId: number) => client.delete(`/sets/${setId}`),

    moveToFolder: (setId: number, folderId: number | null) =>
        client.put(`/sets/${setId}/folder`, { folderId }),
};

export const setFolderApi = {
    getAll: () => client.get<SetFolder[]>("/set-folders"),

    create: (name: string) => client.post<SetFolder>("/set-folders", { name }),

    rename: (folderId: number, name: string) =>
        client.put<SetFolder>(`/set-folders/${folderId}`, { name }),

    delete: (folderId: number) => client.delete(`/set-folders/${folderId}`),
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
    setMetadata: (setId: number, cardId: number, key: string, value: unknown) =>
        client.patch(`/sets/${setId}/cards/${cardId}/metadata`, { key, value }),
};

export const quizApi = {
    getMine: () => client.get<QuizSummary[]>("/quizzes/mine"),
    getPublic: () => client.get<QuizSummary[]>("/quizzes/public"),
    getPublicById: (quizId: number) =>
        client.get<QuizEditorResponse>(`/quizzes/public/${quizId}`),
    getById: (quizId: number) =>
        client.get<QuizEditorResponse>(`/quizzes/${quizId}`),
    create: (request: CreateQuizRequest) =>
        client.post<QuizEditorResponse>("/quizzes", request),
    copyPublic: (quizId: number) =>
        client.post<QuizEditorResponse>(`/quizzes/${quizId}/copy`),
    update: (quizId: number, request: UpdateQuizRequest) =>
        client.put<QuizEditorResponse>(`/quizzes/${quizId}`, request),
    updateVisibility: (quizId: number, isPublic: boolean) =>
        client.patch<QuizEditorResponse>(`/quizzes/${quizId}/visibility`, {
            isPublic,
        }),
    delete: (quizId: number) => client.delete(`/quizzes/${quizId}`),

    addQuestion: (quizId: number, request: QuestionRequest) =>
        client.post<QuestionResponse>(`/quizzes/${quizId}/questions`, request),
    updateQuestion: (questionId: number, request: QuestionRequest) =>
        client.put<QuestionResponse>(
            `/quizzes/questions/${questionId}`,
            request,
        ),
    deleteQuestion: (questionId: number) =>
        client.delete(`/quizzes/questions/${questionId}`),

    addAnswerOption: (questionId: number, request: AnswerOptionRequest) =>
        client.post<AnswerOptionResponse>(
            `/quizzes/questions/${questionId}/answers`,
            request,
        ),
    updateAnswerOption: (
        answerOptionId: number,
        request: AnswerOptionRequest,
    ) =>
        client.put<AnswerOptionResponse>(
            `/quizzes/answers/${answerOptionId}`,
            request,
        ),
    deleteAnswerOption: (answerOptionId: number) =>
        client.delete(`/quizzes/answers/${answerOptionId}`),
};

export const practiceApi = {
    getQuizzes: (filters?: PracticeFilters) =>
        client.get<PracticeQuizSummary[]>("/practice/quizzes", {
            params: filters,
        }),

    getQuiz: (id: number) =>
        client.get<PracticeQuizResponse>(`/practice/quizzes/${id}`),

    checkAnswer: (request: PracticeAnswerRequest) =>
        client.post<PracticeAnswerResponse>("/practice/answer", request),
    getFilters: () => client.get<PracticeFiltersResponse>("/practice/filters"),
};
