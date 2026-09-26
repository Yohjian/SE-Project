import { useState, FormEvent } from "react";
import { useNavigate, Link } from "react-router-dom";
import { authApi } from "../api/client";
import axios from "axios";

export default function Login() {
	const navigate = useNavigate();
	const [email, setEmail] = useState("");
	const [password, setPassword] = useState("");
	const [error, setError] = useState<string | null>(null);
	const [loading, setLoading] = useState(false);

	const handleSubmit = async (e: FormEvent) => {
		e.preventDefault();
		setError(null);
		setLoading(true);
		try {
			const { data } = await authApi.login(email, password);
			sessionStorage.setItem("token_login", data.token);
			navigate("/");
		} catch (err: unknown) {
			if (axios.isAxiosError(err) && err.response?.status === 401) {
				setError("Invalid email or password");
			} else if (axios.isAxiosError(err) && !err.response) {
				setError("Could not reach the server. Please try again.");
			} else {
				setError("Something went wrong. Please try again.");
			}
		} finally {
			setLoading(false);
		}
	};

	return (
		<div className="max-w-sm mx-auto mt-16">
			<h2 className="text-2xl font-bold text-gray-900 mb-6">Log in</h2>
			{error && (
				<div className="mb-4 rounded-md bg-red-50 px-4 py-3 text-sm text-red-700">
					{error}
				</div>
			)}
			<form onSubmit={handleSubmit} className="flex flex-col gap-4">
				<label className="flex flex-col gap-1 text-sm font-medium text-gray-700">
					Email
					<input
						type="email"
						required
						value={email}
						onChange={(e) => setEmail(e.target.value)}
						className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
					/>
				</label>
				<label className="flex flex-col gap-1 text-sm font-medium text-gray-700">
					Password
					<input
						type="password"
						required
						value={password}
						onChange={(e) => setPassword(e.target.value)}
						className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
					/>
				</label>
				<button
					type="submit"
					disabled={loading}
					className="rounded-md bg-indigo-600 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
				>
					{loading ? "Logging in…" : "Log in"}
				</button>
			</form>
			<p className="mt-4 text-center text-sm text-gray-500">
				No account?{" "}
				<Link
					to="/register"
					className="text-indigo-600 hover:underline"
				>
					Register
				</Link>
			</p>
		</div>
	);
}
