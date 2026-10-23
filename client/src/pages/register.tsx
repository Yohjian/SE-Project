import { useState, FormEvent } from "react";
import { useNavigate, Link } from "react-router-dom";
import { authApi } from "../api/client";
import { getErrorMessages } from "../api/auth";
import PasswordInput from "../components/password_input";

import "../styles/auth.css";

const MIN_PASSWORD_LENGTH = 8;

export default function Register() {
    const navigate = useNavigate();
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [repeatPassword, setRepeatPassword] = useState("");
    const [errors, setErrors] = useState<string[]>([]);
    const [loading, setLoading] = useState(false);

    const passwordMismatch =
        repeatPassword.length > 0 && password !== repeatPassword;

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        if (password !== repeatPassword) return;

        setErrors([]);
        setLoading(true);
        try {
            await authApi.register(email.trim(), password);
            navigate("/login", { state: { registered: true } });
        } catch (err: unknown) {
            setErrors(
                getErrorMessages(err, "Registration failed. Please try again."),
            );
        } finally {
            setLoading(false);
        }
    };

    return (
        <>
            <Link to="/" className="btn btn-secondary btn-sm auth-back">
                ← Back
            </Link>
            
            <div className="auth-page">
                <Link to="/" className="auth-logo">
                    QuattroLingo
                </Link>

                <div className="card">
                    <h1 className="page-title">Create an account</h1>

                    {errors.length > 0 && (
                        <div className="alert-error auth-alert" role="alert">
                            {errors.map((msg, i) => (
                                <p key={i}>{msg}</p>
                            ))}
                        </div>
                    )}

                    <form onSubmit={handleSubmit}>
                        <div className="field">
                            <label htmlFor="email">Email</label>
                            <input
                                id="email"
                                type="email"
                                required
                                autoComplete="email"
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
                                minLength={MIN_PASSWORD_LENGTH}
                                autoComplete="new-password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                            />
                            <span className="field-hint">
                                At least {MIN_PASSWORD_LENGTH} characters, with
                                a digit and a lowercase letter.
                            </span>
                        </div>

                        <div className="field">
                            <label htmlFor="repeat-password">
                                Repeat password
                            </label>
                            <PasswordInput
                                id="repeat-password"
                                required
                                minLength={MIN_PASSWORD_LENGTH}
                                autoComplete="new-password"
                                value={repeatPassword}
                                onChange={(e) =>
                                    setRepeatPassword(e.target.value)
                                }
                                className={
                                    passwordMismatch ? "input-error" : ""
                                }
                                aria-invalid={passwordMismatch}
                            />
                            {passwordMismatch && (
                                <span className="field-error" role="alert">
                                    Passwords do not match.
                                </span>
                            )}
                        </div>

                        <button
                            type="submit"
                            disabled={loading || passwordMismatch}
                            className="btn btn-primary btn-block"
                        >
                            {loading ? "Creating account…" : "Register"}
                        </button>
                    </form>

                    <p className="auth-footer text-muted">
                        Already have an account? <Link to="/login">Log in</Link>
                    </p>
                </div>
            </div>
        </>
    );
}
