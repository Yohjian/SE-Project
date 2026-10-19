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
    <div className="dashboard">
      <h1>Welcome</h1>
      <p>Welcome to Quattrolingo</p>


      <section className="public-rooms">
        <h2>Public Rooms</h2>

        {rooms.length === 0 ? (
          <p>No public rooms available.</p>
        ) : (
          <div
            className="public-rooms-list"
          >
            {rooms.map((room) => (
              <div
                key={room.id}
                className="public-room-card"
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
