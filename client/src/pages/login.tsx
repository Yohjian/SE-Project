import { useState, FormEvent } from "react";
import { useNavigate, Link } from "react-router-dom";
import axios from "axios";
import { authApi } from "../api/client";
import { getErrorMessages, saveSession } from "../api/auth";

import "../styles/login.css";

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
      saveSession(data.token, data.role);
      navigate("/dashboard");
    } catch (err: unknown) {
      if (axios.isAxiosError(err) && err.response?.status === 401) {
        setError("Invalid email or password");
      } else {
        setError(
          getErrorMessages(err, "Something went wrong. Please try again.")[0],
        );
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <h2 className="login-title">Log in</h2>
      {error && <div className="login-error">{error}</div>}
      <form onSubmit={handleSubmit} className="login-form">
        <label className="login-label">
          Email
          <input
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="login-input"
          />
        </label>
        <label className="login-label">
          Password
          <input
            type="password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="login-input"
          />
        </label>
        <button type="submit" disabled={loading} className="login-submit">
          {loading ? "Logging in…" : "Log in"}
        </button>
      </form>
      <p className="login-footer">
        No account?{" "}
        <Link to="/register" className="login-link">
          Register
        </Link>
      </p>

      <Link to="/dashboard" className="back-to-dashboard">
        Back to Dashboard
      </Link>
    </div>
  );
}
