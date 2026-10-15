import "../styles/dashboards.css";
import { useEffect, useState } from "react";
import { roomApi, type Room } from "../api/rooms";
import "../styles/rooms.css";

export default function Dashboard() {
  const [rooms, setRooms] = useState<Room[]>([]);
  const [joinCode, setJoinCode] = useState("");

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
                  <p>Room code: {room.joinCode}</p>
                  <p>Participants: {room.participantCount}</p>
                </div>

                <button
                  type="button"
                  className="room-button"
                  onClick={() => {
                    console.log("Join room:", room.joinCode);
                  }}
                >
                  Join Room
                </button>
              </div>
            ))}
          </div>
        )}
      </section>

      <section style={{ marginTop: "32px" }}>
  <h2>Join by Code</h2>

  <p>Enter a room code to join a private room.</p>

  <div
    style={{
      display: "flex",
      gap: "12px",
      marginTop: "16px",
      maxWidth: "420px",
    }}
  >
    <input
      type="text"
      placeholder="Enter room code"
      value={joinCode}
      onChange={(e) =>
        setJoinCode(e.target.value.toUpperCase())
      }
      style={{
        flex: 1,
        minWidth: 0,
        padding: "12px",
        border: "2px solid #8b5cf6",
        borderRadius: "10px",
        fontSize: "16px",
      }}
    />

    <button
      type="button"
      className="room-button"
      disabled={!joinCode.trim()}
    >
      Join
    </button>
  </div>
</section>
    </div>
  );
}
