import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { roomApi, type Room } from "../api/rooms";
import JoinByCode from "../components/join_by_code";

import "../styles/dashboards.css";
import "../styles/rooms.css";

export default function Dashboard() {
  const [rooms, setRooms] = useState<Room[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    roomApi
      .getPublic()
      .then((res) => setRooms(res.data))
      .catch(() => setError("Could not load public rooms."))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="container dashboard">
      <section className="card">
        <h2>Join with a code</h2>
        <JoinByCode />
      </section>

      <section>
        <h2>Public rooms</h2>

        {loading && <p className="text-muted">Loading rooms…</p>}

        {error && (
          <div className="alert-error" role="alert">
            {error}
          </div>
        )}

        {!loading && !error && rooms.length === 0 && (
          <p className="text-muted">No public rooms right now.</p>
        )}

        {rooms.length > 0 && (
          <ul className="room-list">
            {rooms.map((room) => (
              <li key={room.id} className="card">
                <div className="room-row">
                  <div className="room-row__info">
                    <h3 className="room-row__title">{room.quizTitle}</h3>
                    <p className="text-muted">
                      {room.participantCount}{" "}
                      {room.participantCount === 1 ? "player" : "players"}{" "}
                      waiting
                    </p>
                  </div>
                  <Link
                    to={`/rooms/join/${room.joinCode}`}
                    className="btn btn-primary"
                  >
                    Join
                  </Link>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}