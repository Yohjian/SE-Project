import axios from "axios";

const TOKEN_KEY = "token_login";
const ROLE_KEY = "user_role";

export function saveSession(token: string, role: string) {
  sessionStorage.setItem(TOKEN_KEY, token);
  sessionStorage.setItem(ROLE_KEY, role);
}

export function clearSession() {
  sessionStorage.removeItem(TOKEN_KEY);
  sessionStorage.removeItem(ROLE_KEY);
}

export function getToken(): string | null {
  return sessionStorage.getItem(TOKEN_KEY);
}

export function getRole(): string | null {
  return sessionStorage.getItem(ROLE_KEY);
}

export function isLoggedIn(): boolean {
  return getToken() !== null;
}

export function getErrorMessages(err: unknown, fallback: string): string[] {
  if (!axios.isAxiosError(err)) return [fallback];
  if (!err.response) return ["Could not reach the server. Please try again."];

  const data = err.response.data;

  if (data && typeof data === "object") {

    if (data.errors && typeof data.errors === "object") {
      const messages = (Object.values(data.errors) as unknown[])
        .flat()
        .filter((m): m is string => typeof m === "string");
      if (messages.length > 0) return messages;
    }
    if (typeof data.detail === "string") return [data.detail];
  }

  return [fallback];
}