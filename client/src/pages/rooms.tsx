
import { useEffect, useState } from "react";
import { roomApi, type Room } from "../api/rooms";
import { quizApi } from "../api/client";
import type { QuizSummary } from "../api/types";
import "../styles/rooms.css";

export default function Rooms() {
    const [rooms, setRooms] = useState<Room[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    const [showCreateForm, setShowCreateForm] = useState(false);
    const [visibility, setVisibility] = useState(0);

    const [quizzes, setQuizzes] = useState<QuizSummary[]>([]);
    const [selectedQuizId, setSelectedQuizId] = useState("");
    const [creating, setCreating] = useState(false);
    const [createError, setCreateError] = useState("");

    useEffect(() => {
        roomApi
            .getPublic()
            .then((response) => {
                setRooms(response.data);
            })
            .catch(() => {
                setError("Failed to load rooms.");
            })
            .finally(() => {
                setLoading(false);
            });
    }, []);

    useEffect(() => {
        quizApi
            .getMine()
            .then((response) => {
                setQuizzes(response.data);
            })
            .catch((error) => {
                console.error("Failed to load quizzes:", error);
            });
    }, []);


    const handleCreateRoom = async () => {
        if (!selectedQuizId) return;

        setCreating(true);
        setCreateError("");

        try {
            const response = await roomApi.create({
                quizId: Number(selectedQuizId),
                roomVisibility: visibility,
            });

            setRooms((currentRooms) =>
                visibility === 0
                    ? [...currentRooms, response.data]
                    : currentRooms
            );

            setShowCreateForm(false);
            setSelectedQuizId("");
            setVisibility(0);
        } catch (error) {
            console.error("Failed to create room:", error);
            setCreateError("Failed to create room.");
        } finally {
            setCreating(false);
        }
    };

    return (
        <div className="rooms-page">
            <h1 className="rooms-title">Public Rooms</h1>

            {!showCreateForm && (
                <button
                    type="button"
                    className="room-button"
                    onClick={() => setShowCreateForm(true)}
                >
                    Create Room
                </button>
            )}

            {showCreateForm && (
                <div className="create-room-form">
                    <h2>Create a Room</h2>

                    <div className="create-room-field">
                        <label htmlFor="quiz">Choose a quiz</label>
                        <select
                            id="quiz"
                            value={selectedQuizId}
                            onChange={(e) => setSelectedQuizId(e.target.value)}
                        >
                            <option value="">Select a quiz</option>
                            {quizzes.map((quiz) => (
                                <option key={quiz.id} value={quiz.id}>
                                    {quiz.title}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div className="create-room-field">
                        <label htmlFor="roomVisibility">Room visibility</label>
                        <select
                            id="roomVisibility"
                            value={visibility}
                            onChange={(e) => setVisibility(Number(e.target.value))}
                        >
                            <option value={0}>Public</option>
                            <option value={1}>Private</option>
                        </select>
                    </div>

                    <div className="room-actions">
                        <button
                            type="button"
                            className="room-button room-button-secondary"
                            onClick={() => {
                                setShowCreateForm(false);
                                setSelectedQuizId("");
                                setVisibility(0);
                            }}
                        >
                            Cancel
                        </button>

                        <button
                            type="button"
                            className="room-button"
                            onClick={handleCreateRoom}
                            disabled={!selectedQuizId || creating}
                        >
                            {creating ? "Creating..." : "Create Room"}
                        </button>
                    </div>
                    {createError && <p role="alert">{createError}</p>}
                </div>
            )}

            {loading && <p>Loading rooms...</p>}

            {error && <p>{error}</p>}

            {!loading && !error && rooms.length === 0 && (
                <p className="rooms-empty">No public rooms available.</p>
            )}

            {!loading &&
                !error &&
                rooms.map((room) => (
                    <div key={room.id}>
                        <p>Room code: {room.joinCode}</p>
                        <p>Participants: {room.participantCount}</p>
                    </div>
                ))}
        </div>
    );

}
