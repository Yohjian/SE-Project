import { Routes, Route, Navigate } from "react-router-dom";
// pages
import Register from "./pages/register";
import Login from "./pages/login";
import Landing from "./pages/landing";
import Layout from "./components/nav_bar";
import ProtectedRoute from "./components/protected_route";
import GuestRoute from "./components/guest_route";
import Dashboard from "./pages/dashboard";
import Sets from "./pages/sets";
import Quizzes from "./pages/quizzes";
import QuizEditor from "./pages/quizEditor";
import Rooms from "./pages/rooms";
import JoinRoom from "./pages/joinRoom";

export default function App() {
  return (
    <Routes>
      <Route element={<GuestRoute />}>
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
      </Route>

      <Route element={<Layout />}>
        <Route element={<GuestRoute />}>
          <Route path="/" element={<Landing />} />
        </Route>

        <Route path="/rooms/join/:code" element={<JoinRoom />} />

        <Route element={<ProtectedRoute />}>
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/sets" element={<Sets />} />
          <Route path="/quizzes" element={<Quizzes />} />
          <Route path="/quizzes/:id/edit" element={<QuizEditor />} />
          <Route path="/rooms" element={<Rooms />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}