import { useEffect } from "react";
import { Link, useNavigate } from "react-router-dom";
import Login from "./login";

export default function Home() {
  const isLoggedIn = Boolean(sessionStorage.getItem("token_login"));
  const navigate = useNavigate();

  useEffect(() => {
    if (isLoggedIn) {
      navigate("/dashboard");
    }
  }, []);

  if (isLoggedIn) return null; // avoid flash of content before redirect

  return (
    <div>
      <h1>Welcome to QuattroLingo</h1>
      <div>
        <Login />
      </div>
    </div>
  );
}
