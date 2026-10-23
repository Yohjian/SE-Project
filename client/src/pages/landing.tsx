import { Link } from "react-router-dom";
import JoinByCode from "../components/join_by_code";
import "../styles/landing.css";

export default function Landing() {
  return (
    <div className="container landing">
      <h1 className="landing-title">Learn languages by playing together</h1>
      <p className="landing-subtitle">
        Join a live quiz with a room code, or create an account to build your
        own quizzes and host games.
      </p>

      <div className="card landing-join">
        <h2>Have a room code?</h2>
        <JoinByCode />
      </div>

      <div className="landing-actions">
        <Link to="/register" className="btn btn-primary">Create account</Link>
        <Link to="/login" className="btn btn-secondary">Log in</Link>
      </div>
    </div>
  );
}