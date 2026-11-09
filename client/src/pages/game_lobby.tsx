import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { getErrorMessages } from "../api/auth";
import { clearPlayerSession } from "../api/player_session";
import { roomApi } from "../api/rooms";
import { useGameRoom } from "../components/game_room_state";
import "../styles/game.css";

export default function GameLobby() {
    const { code, state, startGame, leaveRoom, setLeaving } = useGameRoom();
    const navigate = useNavigate();
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");

    const handleStart = async () => {
        setBusy(true);
        setError("");
        try {
            await startGame(); // the layout navigates when GameStarted arrives
        } catch (e) {
            setError(e instanceof Error ? e.message : "Could not start the game.");
        } finally {
            setBusy(false);
        }
    };

    const handleClose = async () => {
        setBusy(true);
        setError("");
        setLeaving(true);
        try {
            await roomApi.cancel(code);
            navigate("/rooms");
        } catch (err) {
            setLeaving(false);
            setError(getErrorMessages(err, "Could not close the room.")[0]);
            setBusy(false);
        }
    };

    const handleLeave = async () => {
        setBusy(true);
        setError("");
        setLeaving(true);
        try {
            await leaveRoom();
            clearPlayerSession(code);
            navigate("/");
        } catch (e) {
            setLeaving(false);
            setError(e instanceof Error ? e.message : "Could not leave the room.");
            setBusy(false);
        }
    };

    return (
        <div className="container">
            <div className="lobby card">
                <p className="text-muted">{state.quizTitle}</p>
                <p className="text-muted">
                    Join at {window.location.origin}/rooms/join/{code}
                </p>
                <p className="lobby__code" aria-label="Room code">
                    {code}
                </p>

                <h2>
                    {state.players.length}{" "}
                    {state.players.length === 1 ? "player" : "players"}
                </h2>
                <ul className="lobby__players">
                    {state.players.map((p) => (
                        <li
                            key={p.id}
                            className={`player-chip${p.connected ? "" : " player-chip--offline"}`}
                        >
                            {p.name}
                            {state.me?.id === p.id && " (you)"}
                        </li>
                    ))}
                </ul>

                {error && (
                    <div className="alert-error" role="alert">
                        {error}
                    </div>
                )}

                {state.isHost ? (
                    <div className="lobby__actions">
                        <button
                            type="button"
                            className="btn btn-secondary"
                            onClick={() => void handleClose()}
                            disabled={busy}
                        >
                            Close room
                        </button>
                        <button
                            type="button"
                            className="btn btn-primary"
                            onClick={() => void handleStart()}
                            disabled={busy || state.players.length === 0}
                        >
                            Start game
                        </button>
                    </div>
                ) : (
                    <>
                        <p className="text-muted">Waiting for the host to start…</p>
                        <div className="lobby__actions">
                            <button
                                type="button"
                                className="btn btn-secondary"
                                onClick={() => void handleLeave()}
                                disabled={busy}
                            >
                                Leave
                            </button>
                        </div>
                    </>
                )}
            </div>
        </div>
    );
}