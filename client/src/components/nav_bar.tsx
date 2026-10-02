import { Link, Outlet, useNavigate } from "react-router-dom";
import "../styles/nav_bar.css";
import { NotificationProvider } from "./notification";
import { clearSession } from "../api/auth";

export default function Layout() {
  const navigate = useNavigate();
  const handleLogout = () => {
    clearSession();
    navigate("/login");
  };

  return (
    <NotificationProvider>
      <div className="layout">
        <nav className="navbar">
          <div className="navbar-links">
            <Link to="/dashboard">Dashboard</Link>
            <Link to="/sets">My Sets</Link>
          </div>
          <button className="logout-btn" onClick={handleLogout}>
            Logout
          </button>
        </nav>

        <main className="layout-main">
          <Outlet />
        </main>
      </div>
    </NotificationProvider>
  );
}