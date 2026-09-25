import { Link } from "react-router-dom";

export default function Home() {
	const isLoggedIn = Boolean(sessionStorage.getItem("token_login"));

	return (
		<div className="flex flex-col items-center justify-center py-20 text-center">
			<h1 className="text-4xl font-bold text-gray-900 mb-4">
				Welcome to QuattroLingo
			</h1>
			{isLoggedIn ? (
				<p className="text-gray-500 text-lg mb-8 max-w-md">
					Start learning now.
				</p>
			) : (
				<div className="flex gap-3">
					<Link
						to="/login"
						className="border border-indigo-600 text-indigo-600 px-6 py-3 rounded-lg hover:bg-indigo-50 transition"
					>
						Log in
					</Link>
					<Link
						to="/register"
						className="bg-indigo-600 text-white px-6 py-3 rounded-lg hover:bg-indigo-700 transition"
					>
						Register
					</Link>
				</div>
			)}
		</div>
	);
}
