import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import axios from "axios";
import { roomApi, RoomStatus } from "../api/rooms";
import "../styles/rooms.css";

export default function JoinRoom() {
    const { code } = useParams<{ code: string }>();

    const [displayName, setDisplayName] = useState("");
    const [joining, setJoining] = useState(false);
    const [joined, setJoined] = useState(false);
    const [error, setError] = useState("");
    const [checkingRoom, setCheckingRoom] = useState(true);
    const [roomExists, setRoomExists] = useState(false);

    useEffect(() => {
        let cancelled = false;

        const checkRoom = async () => {
            setCheckingRoom(true);
            setRoomExists(false);
            setError("");

            if (!code) {
                setError("Room code is missing.");
                setCheckingRoom(false);
                return;
            }

            try {
                const { data } = await roomApi.getByCode(code);
                if (cancelled) return;

                if (data.status !== RoomStatus.Waiting) {
                    setError("This game has already started.");
                } else {
                    setRoomExists(true);
                }
            } catch (err: unknown) {
                if (!cancelled) {
                    setError(
                        axios.isAxiosError(err) && err.response?.status === 404
                            ? "Room not found."
                            : "Could not check the room. Please try again.",
                    );
                }
            } finally {
                if (!cancelled) {
                    setCheckingRoom(false);
                }
            }
        };

        void checkRoom();

        return () => {
            cancelled = true;
        };
    }, [code]);

    const handleJoin = async () => {
        if (!code || !displayName.trim()) return;

        setJoining(true);
        setError("");

        try {
            await roomApi.join({
                joinCode: code,
                displayName: displayName.trim(),
            });

            setJoined(true);
        } catch (err) {
            console.error("Failed to join room:", err);
            setError("Could not join room. Please try again.");
        } finally {
            setJoining(false);
        }
    };

    return (
        <div className="rooms-page">
            <h1 className="rooms-title">Join Room</h1>

            <p>
                Room code: <strong>{code}</strong>
            </p>

            {checkingRoom ? (
                <p>Checking room...</p>
            ) : !roomExists ? (
                <p role="alert">{error}</p>
            ) : joined ? (
                <p>Successfully joined the room!</p>
            ) : (
                <div className="create-room-form">
                    <div className="create-room-field">
                        <label htmlFor="displayName">Your name</label>

                        <input
                            id="displayName"
                            type="text"
                            placeholder="Enter your name"
                            value={displayName}
                            onChange={(e) => setDisplayName(e.target.value)}
                            maxLength={30}
                        />
                    </div>

                    <button
                        type="button"
                        className="room-button"
                        onClick={handleJoin}
                        disabled={joining || !displayName.trim() || !code}
                    >
                        {joining ? "Joining..." : "Join Room"}
                    </button>

                    {error && <p role="alert">{error}</p>}
                </div>
            )}
        </div>
    );
}
