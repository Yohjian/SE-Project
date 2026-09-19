import { Routes, Route } from "react-router-dom";

// pages
import Home from "./pages/home";
import Register from "./pages/register";
import Login from "./pages/login";

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Home />} />
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
    </Routes>
  );
}
