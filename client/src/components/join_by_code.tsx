import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";

export default function JoinByCode() {
  const navigate = useNavigate();
  const [code, setCode] = useState("");

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    const trimmed = code.trim();
    if (!trimmed) return;
    navigate(`/rooms/join/${encodeURIComponent(trimmed)}`);
  };

  return (
    <form className="join-form" onSubmit={handleSubmit}>
      <input
        className="input"
        value={code}
        onChange={(e) => setCode(e.target.value)}
        placeholder="Enter room code"
        aria-label="Room code"
      />
      <button className="btn btn-primary" type="submit" disabled={!code.trim()}>
        Join
      </button>
    </form>
  );
}