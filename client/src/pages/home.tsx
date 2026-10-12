import { Link } from "react-router-dom";

import "../styles/home.css";

export default function Home() {
  const isLoggedIn = Boolean(sessionStorage.getItem("token_login"));

  return (
    <div className="home-hero">
      <h1 className="home-title">Welcome to QuattroLingo</h1>
      {isLoggedIn ? (
        <p className="home-subtitle">Start learning now.</p>
      ) : (
        <div className="home-actions">
          <Link to="/login" className="home-btn home-btn--outline">
            Log in
          </Link>
          <Link to="/register" className="home-btn home-btn--primary">
            Register
          </Link>
        </div>
      )}
    </div>
  );
}
