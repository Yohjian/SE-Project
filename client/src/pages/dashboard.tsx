import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { roomApi, type Room } from "../api/rooms";
import { quizApi } from "../api/client";
import type { QuizSummary } from "../api/types";
import JoinByCode from "../components/join_by_code";
import { useNotification } from "../components/notification";

import "../styles/dashboards.css";
import "../styles/rooms.css";

export default function Dashboard() {
  const [rooms, setRooms] = useState<Room[]>([]);
  const [quizzes, setQuizzes] = useState<QuizSummary[]>([]);
  const [savingQuizId, setSavingQuizId] = useState<number | null>(null);
  const { showNotification } = useNotification();
  const [quizSearch, setQuizSearch] = useState("");
  const [quizzesLoading, setQuizzesLoading] = useState(true);
  const [quizzesError, setQuizzesError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    roomApi
      .getPublic()
      .then((res) => setRooms(res.data))
      .catch(() => setError("Could not load public rooms."))
      .finally(() => setLoading(false));
    quizApi
      .getPublic()
      .then((res) => setQuizzes(res.data))
      .catch(() => setQuizzesError("Could not load public quizzes."))
      .finally(() => setQuizzesLoading(false));
  }, []);
    const filteredQuizzes = quizzes.filter((quiz) =>
      quiz.title.toLowerCase().includes(quizSearch.trim().toLowerCase()),
  );

    const handleSaveQuiz = async (quizId: number) => {
      setSavingQuizId(quizId);

      try {
          await quizApi.copyPublic(quizId);

          showNotification("Quiz saved to My Quizzes!", "success");
      } catch {
          showNotification("Failed to save quiz.");
      } finally {
          setSavingQuizId(null);
      }
  };

  return (
    <div className="container dashboard">
      <div className="dashboard-columns">
          <div className="dashboard-column">
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

            <div className="dashboard-column">
                <section className="card">
                    <h2>Search quizzes</h2>

                    <input
                        type="search"
                        className="input"
                        placeholder="Enter quiz name"
                        aria-label="Search public quizzes"
                        value={quizSearch}
                        onChange={(event) => setQuizSearch(event.target.value)}
                    />
                </section>

                <section>
                    <h2>Public quizzes</h2>

                    {quizzesLoading && (
                        <p className="text-muted">Loading quizzes…</p>
                    )}

                    {quizzesError && (
                        <div className="alert-error" role="alert">
                            {quizzesError}
                        </div>
                    )}

                    {!quizzesLoading &&
                        !quizzesError &&
                        filteredQuizzes.length === 0 && (
                            <p className="text-muted">
                                {quizSearch.trim()
                                    ? "No matching public quizzes found."
                                    : "No public quizzes available."}
                            </p>
                        )}

                    {!quizzesLoading && !quizzesError && filteredQuizzes.length > 0 && (
                        <ul className="room-list">
                            {filteredQuizzes.map((quiz) => (
                                <li key={quiz.id} className="card">
                                    <div className="room-row">
                                        <div className="room-row__info">
                                            <h3 className="room-row__title">
                                                {quiz.title}
                                            </h3>

                                            {quiz.description && (
                                                <p className="text-muted">
                                                    {quiz.description}
                                                </p>
                                            )}
                                                                                    </div>

                                        <button
                                            type="button"
                                            className="btn btn-primary"
                                            onClick={() => handleSaveQuiz(quiz.id)}
                                            disabled={savingQuizId !== null}
                                        >
                                            {savingQuizId === quiz.id
                                                ? "Saving..."
                                                : "Save"}
                                        </button>
                                    </div>
                                </li>
                            ))}
                        </ul>
                    )}
                </section>
            </div>
        </div>
    </div>
  );
}