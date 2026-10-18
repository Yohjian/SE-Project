import { useState, FormEvent } from "react";
import { useNavigate, Link } from "react-router-dom";
import { authApi } from "../api/client";
import { getErrorMessages } from "../api/auth";
import "../styles/login.css";

import "../styles/register.css";

export default function Register() {
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [repeatPassword, setRepeatPassword] = useState("");
  const [passwordMismatch, setPasswordMismatch] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setErrors([]);
    setLoading(true);

    if (passwordMismatch) {
      setLoading(false);
      return;
    }

    try {
      await authApi.register(email, password);
      navigate("/login");
    } catch (err: unknown) {
      setErrors(
        getErrorMessages(err, "Registration failed. Please try again."),
      );
    } finally {
      setLoading(false);
    }
  };

  const handleRepeatChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setRepeatPassword(value);
    setPasswordMismatch(value !== password);
  };

  const handlePasswordChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setPassword(value);
    if (repeatPassword.length > 0) {
      setPasswordMismatch(value !== repeatPassword);
    }
  };

  return (
    <div className="register-page">
      <h2 className="register-title">Create an account</h2>

      {errors.length > 0 && (
        <div className="register-errors">
          {errors.map((msg, i) => (
            <p key={i}>{msg}</p>
          ))}
        </div>
      )}

      <form onSubmit={handleSubmit} className="register-form">
        <label className="register-label">
          Email
          <input
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="register-input"
          />
        </label>
        <label className="register-label">
          Password
          <input
            type="password"
            required
            minLength={8}
            value={password}
            onChange={handlePasswordChange}
            className="register-input"
          />
        </label>
        <label className="register-label">
          Repeat password
          <input
            type="password"
            required
            minLength={8}
            value={repeatPassword}
            onChange={handleRepeatChange}
            className={`register-input ${passwordMismatch ? "register-input--error" : ""}`}
          />
          {passwordMismatch && (
            <p className="register-mismatch">Passwords do not match.</p>
          )}
        </label>

        <button type="submit" disabled={loading} className="register-submit">
          {loading ? "Creating account…" : "Register"}
        </button>
      </form>

      <p className="register-footer">
        Already have an account?{" "}
        <Link to="/login" className="register-link">
          Log in
        </Link>
      </p>

      <Link to="/dashboard" className="back-to-dashboard">
        Back to Dashboard
      </Link>
    </div>
  );
}
