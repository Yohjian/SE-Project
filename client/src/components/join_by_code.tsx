import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import axios from "axios";
import { roomApi, RoomStatus } from "../api/rooms";

const CODE_LENGTH = 6;

export default function JoinByCode() {
    const navigate = useNavigate();
    const [code, setCode] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [checking, setChecking] = useState(false);

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        if (!code) return;

        setError(null);
        setChecking(true);
        try {
            const { data } = await roomApi.getByCode(code);
            if (data.status !== RoomStatus.Waiting) {
                setError("This game has already started.");
                return;
            }
            navigate(`/rooms/join/${encodeURIComponent(code)}`);
        } catch (err: unknown) {
            setError(
                axios.isAxiosError(err) && err.response?.status === 404
                    ? "Room not found."
                    : "Could not check the room. Please try again.",
            );
        } finally {
            setChecking(false);
        }
    };

    return (
        <div>
            <form className="join-form" onSubmit={handleSubmit}>
                <input
                    className={`input ${error ? "input-error" : ""}`}
                    value={code}
                    onChange={(e) => {
                        setCode(e.target.value.replace(/\D/g, ""));
                        setError(null);
                    }}
                    inputMode="numeric"
                    maxLength={CODE_LENGTH}
                    autoComplete="off"
                    placeholder="Enter room code"
                    aria-label="Room code"
                    aria-invalid={error !== null}
                />
                <button
                    className="btn btn-primary"
                    type="submit"
                    disabled={!code || checking}
                >
                    {checking ? "Checking…" : "Join"}
                </button>
            </form>
            {error && (
                <p className="field-error" role="alert">
                    {error}
                </p>
            )}
        </div>
    );
}
