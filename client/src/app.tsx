import { Routes, Route } from "react-router-dom";

// pages
import Home from "./pages/home";
import Register from "./pages/register";
import Login from "./pages/login";
import Layout from "./components/nav_bar";
import Dashboard from "./pages/dashboard";
import Sets from "./pages/sets";

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Home />} />
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      <Route element={<Layout />}>
        <Route path="/sets" element={<Sets />} />
        <Route path="/dashboard" element={<Dashboard />} />
      </Route>
    </Routes>
  );
}
