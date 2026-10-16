import "../styles/dashboards.css";
import { useEffect, useState } from "react";
import { roomApi, type Room } from "../api/rooms";
import "../styles/rooms.css";
import { useNavigate } from "react-router-dom";

export default function Dashboard() {
  const [rooms, setRooms] = useState<Room[]>([]);
  const [joinCode, setJoinCode] = useState("");
  const navigate = useNavigate();


  useEffect(() => {
    roomApi.getPublic()
      .then((response) => setRooms(response.data))
      .catch((error) => console.error("Failed to load rooms:", error));
  }, []);
  return (
    <div className="dashboard" style={{ padding: "2rem" }}>
      <h1>Dashboard</h1>
      <p>Welcome back!</p>





      <section style={{ marginTop: "32px" }}>
        <h2>Public Rooms</h2>

        {rooms.length === 0 ? (
          <p>No public rooms available.</p>
        ) : (
          <div
            style={{
              display: "flex",
              flexDirection: "column",
              gap: "16px",
              marginTop: "16px",
            }}
          >
            {rooms.map((room) => (
              <div
                key={room.id}
                style={{
                  border: "2px solid #8b5cf6",
                  borderRadius: "12px",
                  padding: "16px",
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  gap: "16px",
                }}
              >
                <div>
                  <h3>{room.quizTitle}</h3>
                </div>


                <button
                  className="room-button"
                  onClick={() => navigate(`/rooms/join/${room.joinCode}`)}
                >
                  Join Room
                </button>

              </div>
            ))}
          </div>
        )}
      </section>

      <section className="join-by-code">
        <h2>Join by Code</h2>

        <div className="join-by-code-controls">
          <input
            type="text"
            placeholder="Enter room code"
            aria-label="Room code"
            value={joinCode}
            onChange={(e) => setJoinCode(e.target.value)}
            maxLength={6}
          />

          <button
            type="button"
            className="room-button"
            disabled={!joinCode.trim()}
            onClick={() => navigate(`/rooms/join/${joinCode.trim()}`)}
          >
            Join Room
          </button>
        </div>
      </section>
    </div>
  );
}
