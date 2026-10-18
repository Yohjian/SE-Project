import { Link, Outlet, useNavigate } from "react-router-dom";
import "../styles/nav_bar.css";
import { NotificationProvider } from "./notification";
import { clearSession, getToken } from "../api/auth";

export default function Layout() {
  const navigate = useNavigate();
  const isLoggedIn = Boolean(getToken());
  const handleLogout = () => {
    clearSession();
    navigate("/dashboard", { replace: true });
  };

  return (
    <NotificationProvider>
      <div className="layout">
        <nav className="navbar">
          <div className="navbar-links">
            <Link to="/dashboard">Dashboard</Link>
            {isLoggedIn && (
              <>
                <Link to="/sets">My Sets</Link>
                <Link to="/quizzes">Quizzes</Link>
                <Link to="/rooms">Rooms</Link>
              </>
            )}
          </div>

          {isLoggedIn ? (
            <button className="logout-btn" onClick={handleLogout}>
              Logout
            </button>
          ) : (
            <div className="navbar-links">
              <Link to="/login">Login</Link>
              <Link to="/register">Register</Link>
            </div>
          )}
        </nav>

        <main className="layout-main">
          <Outlet />
        </main>
      </div>
    </NotificationProvider>
  );
}