import axios from "axios";
import { CardResponse, SetSummary } from "./types";
import { clearSession, getToken } from "./auth";

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