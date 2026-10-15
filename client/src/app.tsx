import { Routes, Route } from "react-router-dom";
// pages
import Home from "./pages/home";
import Register from "./pages/register";
import Login from "./pages/login";
import Layout from "./components/nav_bar";
import ProtectedRoute from "./components/protected_route";
import Dashboard from "./pages/dashboard";
import Sets from "./pages/sets";
import Quizzes from "./pages/quizzes";
import QuizEditor from "./pages/quizEditor";
import Rooms from "./pages/rooms";
import JoinRoom from "./pages/join_room";

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Home />} />
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />

      {/* Viešas Join Room puslapis */}
      <Route
        path="/rooms/join/:code"
        element={<JoinRoom />}
      />

      {/* Puslapiai prisijungusiems naudotojams */}
      <Route element={<ProtectedRoute />}>
        <Route element={<Layout />}>
          <Route path="/sets" element={<Sets />} />
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/quizzes" element={<Quizzes />} />
          <Route path="/quizzes/:id/edit" element={<QuizEditor />} />
          <Route path="/rooms" element={<Rooms />} />
        </Route>
      </Route>
    </Routes>
  );
}
