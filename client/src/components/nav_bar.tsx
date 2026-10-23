import { Link, NavLink, Outlet, useNavigate } from "react-router-dom";
import "../styles/nav_bar.css";
import { NotificationProvider } from "./notification";
import { clearSession, isLoggedIn } from "../api/auth";

export default function Layout() {
  const navigate = useNavigate();
  const loggedIn = isLoggedIn();

  const handleLogout = () => {
    clearSession();
    navigate("/", { replace: true });
  };

  return (
    <NotificationProvider>
      <div className="layout">
        <nav className="navbar">
          <div className="navbar-left">
            <Link to={loggedIn ? "/dashboard" : "/"} className="navbar-brand">
              QuattroLingo
            </Link>
            {loggedIn && (
              <div className="navbar-links">
                <NavLink to="/dashboard">Dashboard</NavLink>
                <NavLink to="/sets">My Sets</NavLink>
                <NavLink to="/quizzes">Quizzes</NavLink>
                <NavLink to="/rooms">Rooms</NavLink>
              </div>
            )}
          </div>

          <div className="navbar-actions">
            {loggedIn ? (
              <button className="btn btn-secondary" onClick={handleLogout}>
                Logout
              </button>
            ) : (
              <>
                <Link to="/login" className="btn btn-secondary">Login</Link>
                <Link to="/register" className="btn btn-primary">Register</Link>
              </>
            )}
          </div>
        </nav>

        <main className="layout-main">
          <Outlet />
        </main>
      </div>
    </NotificationProvider>
  );
}