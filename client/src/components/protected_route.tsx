import { Navigate, Outlet } from "react-router-dom";
import { isLoggedIn } from "../api/auth";

export default function ProtectedRoute() {
  return isLoggedIn() ? <Outlet /> : <Navigate to="/login" replace />;
}