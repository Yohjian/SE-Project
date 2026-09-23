import { useState, FormEvent } from "react";
import { useNavigate, Link } from "react-router-dom";
import axios from "axios";
import { authApi } from "../api/client";

export default function Register() {
	const navigate = useNavigate();
	const [email, setEmail] = useState("");
	const [password, setPassword] = useState("");
	const [errors, setErrors] = useState<string[]>([]);
	const [loading, setLoading] = useState(false);
	const [passwordMatch, setPasswordMatch] = useState(false);

	const handleSubmit = async (e: FormEvent) => {
		e.preventDefault();
		setErrors([]);
		setLoading(true);

		if (!passwordMatch) {
			setLoading(false);
			return;
		}

		try {
			await authApi.register(email, password);
			navigate("/login");
		} catch (err: unknown) {
			if (axios.isAxiosError(err) && err.response?.data) {
				const body = err.response.data;

				if (Array.isArray(body)) {
					setErrors(
						body.map(
							(e: { description?: string }) =>
								e.description ?? JSON.stringify(e),
						),
					);
				} else if (typeof body === "string") {
					setErrors([body]);
				} else {
					setErrors(["Registration failed. Please try again."]);
				}
			} else {
				setErrors(["Could not reach the server."]);
			}
		} finally {
			setLoading(false);
		}
	};

	const comparePasswords = async (value: string) => {
		if (value === password) {
			setPasswordMatch(true);
			setErrors([]);
		} else {
			setErrors(["Passwords do not match"]);
		}
	};

	return (
		<div className="max-w-sm mx-auto mt-16">
			<h2 className="text-2xl font-bold text-gray-900 mb-6">
				Create an account
			</h2>

			{errors.length > 0 && (
				<div className="mb-4 rounded-md bg-red-50 px-4 py-3 text-sm text-red-700 space-y-1">
					{errors.map((msg, i) => (
						<p key={i}>{msg}</p>
					))}
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
						minLength={8}
						value={password}
						onChange={(e) => setPassword(e.target.value)}
						className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
					/>
				</label>
				<label className="flex flex-col gap-1 text-sm font-medium text-gray-700">
					Repeat password
					<span className="font-normal text-gray-400 text-xs">
						Repeat password
					</span>
					<input
						type="password"
						required
						minLength={8}
						onChange={(e) => comparePasswords(e.target.value)}
						className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
					/>
				</label>

				<button
					type="submit"
					disabled={loading}
					className="rounded-md bg-indigo-600 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
				>
					{loading ? "Creating account…" : "Register"}
				</button>
			</form>
			<p className="mt-4 text-center text-sm text-gray-500">
				Already have an account?{" "}
				<Link to="/login" className="text-indigo-600 hover:underline">
					Log in
				</Link>
			</p>
		</div>
	);
}
