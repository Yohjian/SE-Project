import axios from "axios";

const baseURL = import.meta.env.VITE_API_BASE_URL ?? "/api";

const client = axios.create({
	baseURL,
	headers: { "Content-Type": "application/json" },
});

client.interceptors.request.use((config) => {
	const login_token = sessionStorage.getItem("token_login");
	if (login_token) {
		config.headers.Authorization = `Bearer ${login_token}`;
	}
	return config;
});

client.interceptors.response.use(
	(response) => response,
	(error) => {
		const isAuthEndpoint = error.config?.url?.includes("/auth/login") ||
			error.config?.url?.includes("/auth/register");

		if (error.response?.status === 401 && !isAuthEndpoint) {
			sessionStorage.removeItem("token_login");
			window.location.href = "/login";
		}
		return Promise.reject(error);
	},
);
export interface AuthResponse {
	token: string;
	email: string;
}
export default client;
export const authApi = {
	register: (email: string, password: string) =>
		client.post<{ message: string }>("/auth/register", { email, password }),
	login: (email: string, password: string) =>
		client.post<{ token: string; email: string }>("/auth/login", {
			email,
			password,
		}),
};
