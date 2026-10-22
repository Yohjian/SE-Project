import { useEffect, useState, type FormEvent } from "react";
import { Link, useParams } from "react-router-dom";
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

    const handleJoin = async (e: FormEvent) => {
        e.preventDefault();
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
        <div className="container">
            <div className="join-room">
                <h1 className="page-title">Join room</h1>

                <div className="card">
                    <p className="text-muted">Room code</p>
                    <p className="join-room__code">{code}</p>

                    {checkingRoom ? (
                        <p className="text-muted">Checking room…</p>
                    ) : !roomExists ? (
                        <>
                            <div className="alert-error" role="alert">
                                {error}
                            </div>
                            <Link
                                to="/"
                                className="btn btn-secondary btn-block"
                            >
                                Back to home
                            </Link>
                        </>
                    ) : joined ? (
                        <div className="alert-success" role="status">
                            Successfully joined the room!
                        </div>
                    ) : (
                        <form onSubmit={handleJoin}>
                            <div className="field">
                                <label htmlFor="displayName">Your name</label>
                                <input
                                    id="displayName"
                                    type="text"
                                    className="input"
                                    placeholder="Enter your name"
                                    autoFocus
                                    value={displayName}
                                    onChange={(e) =>
                                        setDisplayName(e.target.value)
                                    }
                                    maxLength={30}
                                />
                            </div>

                            {error && (
                                <div className="alert-error" role="alert">
                                    {error}
                                </div>
                            )}

                            <button
                                type="submit"
                                className="btn btn-primary btn-block"
                                disabled={
                                    joining || !displayName.trim() || !code
                                }
                            >
                                {joining ? "Joining…" : "Join room"}
                            </button>
                        </form>
                    )}
                </div>
            </div>
        </div>
    );
}
