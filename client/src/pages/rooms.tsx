import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { roomApi, RoomStatus, RoomVisibility, type Room } from "../api/rooms";
import { quizApi } from "../api/client";
import { getErrorMessages } from "../api/auth";
import type { QuizSummary } from "../api/types";

import "../styles/rooms.css";

const STATUS_LABELS: Record<number, string> = {
    [RoomStatus.Waiting]: "Waiting",
    [RoomStatus.InProgress]: "In progress",
    [RoomStatus.Finished]: "Finished",
    [RoomStatus.Cancelled]: "Cancelled",
};

export default function Rooms() {
    const [rooms, setRooms] = useState<Room[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const [showCreateForm, setShowCreateForm] = useState(false);
    const [visibility, setVisibility] = useState<number>(RoomVisibility.Public);
    const [quizzes, setQuizzes] = useState<QuizSummary[]>([]);
    const [quizzesLoading, setQuizzesLoading] = useState(true);
    const [quizzesError, setQuizzesError] = useState<string | null>(null);
    const [selectedQuizId, setSelectedQuizId] = useState("");
    const [creating, setCreating] = useState(false);
    const [createError, setCreateError] = useState<string | null>(null);

    useEffect(() => {
        roomApi
            .getMine()
            .then((res) => setRooms(res.data))
            .catch(() => setError("Failed to load your rooms."))
            .finally(() => setLoading(false));
    }, []);

    useEffect(() => {
        quizApi
            .getMine()
            .then((res) => setQuizzes(res.data))
            .catch(() => setQuizzesError("Could not load your quizzes."))
            .finally(() => setQuizzesLoading(false));
    }, []);

    const closeForm = () => {
        setShowCreateForm(false);
        setSelectedQuizId("");
        setVisibility(RoomVisibility.Public);
        setCreateError(null);
    };

    const handleCreateRoom = async (e: FormEvent) => {
        e.preventDefault();
        if (!selectedQuizId) return;

        setCreating(true);
        setCreateError(null);
        try {
            const { data } = await roomApi.create({
                quizId: Number(selectedQuizId),
                roomVisibility: visibility,
            });
            setRooms((current) => [data, ...current]);
            closeForm();
        } catch (err: unknown) {
            setCreateError(getErrorMessages(err, "Failed to create room.")[0]);
        } finally {
            setCreating(false);
        }
    };

    const noQuizzes = !quizzesLoading && !quizzesError && quizzes.length === 0;

    return (
        <div className="container">
            <header className="rooms-header">
                <h1 className="page-title">My rooms</h1>
                {!showCreateForm && (
                    <button
                        type="button"
                        className="btn btn-primary"
                        onClick={() => setShowCreateForm(true)}
                    >
                        Create room
                    </button>
                )}
            </header>

            {showCreateForm && (
                <form className="card rooms-form" onSubmit={handleCreateRoom}>
                    <h2>Create a room</h2>

                    {quizzesError && (
                        <div className="alert-error rooms-alert" role="alert">
                            {quizzesError}
                        </div>
                    )}

                    {noQuizzes ? (
                        <p className="text-muted">
                            You need a quiz before you can host a room.{" "}
                            <Link to="/quizzes" className="rooms-link">
                                Create one
                            </Link>
                        </p>
                    ) : (
                        <>
                            <div className="field">
                                <label htmlFor="quiz">Quiz</label>
                                <select
                                    id="quiz"
                                    className="input"
                                    value={selectedQuizId}
                                    onChange={(e) =>
                                        setSelectedQuizId(e.target.value)
                                    }
                                >
                                    <option value="">Select a quiz</option>
                                    {quizzes.map((quiz) => (
                                        <option key={quiz.id} value={quiz.id}>
                                            {quiz.title}
                                        </option>
                                    ))}
                                </select>
                            </div>

                            <div className="field">
                                <label htmlFor="roomVisibility">
                                    Visibility
                                </label>
                                <select
                                    id="roomVisibility"
                                    className="input"
                                    value={visibility}
                                    onChange={(e) =>
                                        setVisibility(Number(e.target.value))
                                    }
                                >
                                    <option value={RoomVisibility.Public}>
                                        Public
                                    </option>
                                    <option value={RoomVisibility.Private}>
                                        Private
                                    </option>
                                </select>
                                <span className="field-hint">
                                    Public rooms appear on the dashboard.
                                    Private rooms can only be joined with the
                                    room code.
                                </span>
                            </div>
                        </>
                    )}

                    {createError && (
                        <div className="alert-error rooms-alert" role="alert">
                            {createError}
                        </div>
                    )}

                    <div className="rooms-form__actions">
                        <button
                            type="button"
                            className="btn btn-secondary"
                            onClick={closeForm}
                            disabled={creating}
                        >
                            Cancel
                        </button>
                        <button
                            type="submit"
                            className="btn btn-primary"
                            disabled={!selectedQuizId || creating}
                        >
                            {creating ? "Creating…" : "Create room"}
                        </button>
                    </div>
                </form>
            )}

            {loading && <p className="text-muted">Loading rooms…</p>}

            {error && (
                <div className="alert-error" role="alert">
                    {error}
                </div>
            )}

            {!loading && !error && rooms.length === 0 && (
                <p className="text-muted">You have no rooms yet.</p>
            )}

            {rooms.length > 0 && (
                <ul className="room-list">
                    {rooms.map((room) => (
                        <li key={room.id} className="card">
                            <div className="room-row">
                                <div className="room-row__info">
                                    <h3 className="room-row__title">
                                        {room.quizTitle}
                                    </h3>
                                    <p className="text-muted">
                                        {room.visibility ===
                                        RoomVisibility.Public
                                            ? "Public"
                                            : "Private"}
                                        {" · "}
                                        {STATUS_LABELS[room.status] ??
                                            "Unknown"}
                                        {" · "}
                                        {room.participantCount}{" "}
                                        {room.participantCount === 1
                                            ? "player"
                                            : "players"}
                                    </p>
                                </div>
                                <div
                                    className="room-code"
                                    aria-label="Room code"
                                >
                                    {room.joinCode}
                                </div>
                            </div>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}
