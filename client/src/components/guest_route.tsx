import { Navigate, Outlet } from "react-router-dom";
import { isLoggedIn } from "../api/auth";

export default function GuestRoute() {
  return isLoggedIn() ? <Navigate to="/dashboard" replace /> : <Outlet />;
}