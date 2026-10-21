import { useState, FormEvent } from "react";
import { useNavigate, useLocation, Link } from "react-router-dom";
import axios from "axios";
import { authApi } from "../api/client";
import { getErrorMessages, saveSession } from "../api/auth";
import PasswordInput from "../components/password_input";

import "../styles/auth.css";

export default function Login() {
    const navigate = useNavigate();
    const location = useLocation();
    const justRegistered = (location.state as { registered?: boolean } | null)
        ?.registered;

    const [showSuccess, setShowSuccess] = useState(Boolean(justRegistered));
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setShowSuccess(false);
        setError(null);
        setLoading(true);
        try {
            const { data } = await authApi.login(email.trim(), password);
            saveSession(data.token, data.role);
            navigate("/dashboard");
        } catch (err: unknown) {
            if (axios.isAxiosError(err) && err.response?.status === 401) {
                setError("Invalid email or password");
            } else {
                setError(
                    getErrorMessages(
                        err,
                        "Something went wrong. Please try again.",
                    )[0],
                );
            }
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="auth-page">
            <Link to="/" className="auth-logo">
                QuattroLingo
            </Link>

            <div className="card">
                <h1 className="page-title">Log in</h1>

                {showSuccess && (
                    <div className="alert-success auth-alert" role="status">
                        Account created successfully!
                    </div>
                )}
                {error && (
                    <div className="alert-error auth-alert" role="alert">
                        {error}
                    </div>
                )}

                <form onSubmit={handleSubmit}>
                    <div className="field">
                        <label htmlFor="email">Email</label>
                        <input
                            id="email"
                            type="email"
                            required
                            autoComplete="username"
                            autoFocus
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            className="input"
                        />
                    </div>

                    <div className="field">
                        <label htmlFor="password">Password</label>
                        <PasswordInput
                            id="password"
                            required
                            autoComplete="current-password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                        />
                    </div>

                    <button
                        type="submit"
                        disabled={loading}
                        className="btn btn-primary btn-block"
                    >
                        {loading ? "Logging in…" : "Log in"}
                    </button>
                </form>

                <p className="auth-footer text-muted">
                    No account? <Link to="/register">Register</Link>
                </p>
            </div>
        </div>
    );
}
