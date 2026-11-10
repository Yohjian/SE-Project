import { useEffect, useReducer, useRef, useState } from "react";
import {
    Link,
    Navigate,
    Outlet,
    useLocation,
    useNavigate,
    useParams,
} from "react-router-dom";
import { getToken } from "../api/auth";
import { GameHub, hubErrorMessage } from "../api/game_hub";
import { GamePhase, type GamePhaseValue } from "../api/game_types";
import { clearPlayerSession, getPlayerSession } from "../api/player_session";
import {
    GameRoomContext,
    gameRoomReducer,
    type GameRoomContextValue,
} from "./game_room_state";
import "../styles/game.css";

function pageFor(phase: GamePhaseValue): "lobby" | "play" | "results" | null {
    switch (phase) {
        case GamePhase.Waiting:
            return "lobby";
        case GamePhase.Countdown:
        case GamePhase.QuestionOpen:
        case GamePhase.QuestionReview:
            return "play";
        case GamePhase.Finished:
            return "results";
        default:
            return null;
    }
}

function Notice({ message }: { message: string }) {
    return (
        <div className="container">
            <div className="card game-notice">
                <div className="alert-error" role="alert">
                    {message}
                </div>
                <Link to="/" className="btn btn-secondary btn-block">
                    Back to home
                </Link>
            </div>
        </div>
    );
}

export default function GameRoomLayout() {
    const { code = "" } = useParams<{ code: string }>();
    const navigate = useNavigate();
    const { pathname } = useLocation();

    // A stored player session means player; otherwise a logged-in user is the host.
    const playerToken = getPlayerSession(code)?.playerToken ?? null;
    const canConnect = playerToken !== null || !!getToken();

    const [state, dispatch] = useReducer(gameRoomReducer, null);
    const [error, setError] = useState("");
    const [reconnecting, setReconnecting] = useState(false);
    const hubRef = useRef<GameHub | null>(null);
    const leavingRef = useRef(false);

    useEffect(() => {
        if (!canConnect) return;

        let disposed = false;

        const hub = new GameHub(
            { joinCode: code, playerToken },
            {
                onSnapshot: (snapshot) =>
                    dispatch({ type: "snapshot", snapshot }),
                onPlayerJoined: (player) =>
                    dispatch({ type: "playerJoined", player }),
                onPlayerLeft: (participantId) =>
                    dispatch({ type: "playerLeft", participantId }),
                onGameStarted: (info) =>
                    dispatch({ type: "gameStarted", ...info }),
                onQuestionStarted: (question) =>
                    dispatch({ type: "questionStarted", question }),
                onQuestionEnded: (result) =>
                    dispatch({ type: "questionEnded", result }),
                onScoreUpdated: (leaderboard) =>
                    dispatch({ type: "scoreUpdated", leaderboard }),
                onGameFinished: (leaderboard) =>
                    dispatch({ type: "gameFinished", leaderboard }),
                onGameCancelled: () => {
                    if (!leavingRef.current) dispatch({ type: "cancelled" });
                },
                onReconnecting: () => setReconnecting(true),
                onReconnected: () => setReconnecting(false),
                onConnectionLost: () => {
                    if (!disposed)
                        setError(
                            "Connection lost. Reload the page to try again.",
                        );
                },
            },
        );
        hubRef.current = hub;

        hub.start().catch((e: unknown) => {
            if (!disposed) {
                setError(
                    e instanceof Error
                        ? e.message
                        : "Could not enter this room.",
                );
            }
        });

        return () => {
            disposed = true;
            void hub.stop();
        };
    }, [code, playerToken, canConnect]);

    useEffect(() => {
        if (state?.phase === GamePhase.Cancelled) clearPlayerSession(code);
    }, [state?.phase, code]);

    const target = state ? pageFor(state.phase) : null;
    useEffect(() => {
        if (target && !pathname.endsWith(`/${target}`)) {
            navigate(`/rooms/${code}/${target}`, { replace: true });
        }
    }, [target, pathname, code, navigate]);

    if (!canConnect) return <Navigate to={`/rooms/join/${code}`} replace />;
    if (error) return <Notice message={error} />;
    if (!state) {
        return (
            <div className="container">
                <p className="text-muted">Connecting…</p>
            </div>
        );
    }
    if (state.phase === GamePhase.Cancelled) {
        return <Notice message="The host closed this room." />;
    }

    const call = async (action: (hub: GameHub) => Promise<unknown>) => {
        const hub = hubRef.current;
        if (!hub) return;
        try {
            await action(hub);
        } catch (e) {
            throw new Error(hubErrorMessage(e));
        }
    };

    const value: GameRoomContextValue = {
        code,
        state,
        reconnecting,
        startGame: () => call((hub) => hub.startGame()),
        leaveRoom: () => call((hub) => hub.leaveRoom()),
        submitAnswer: async (optionId) => {
            const question = state.question;
            if (!question) return;
            dispatch({ type: "answered", optionId });
            try {
                await call((hub) => hub.submitAnswer(question.index, optionId));
            } catch (e) {
                const alreadyAnswered =
                    e instanceof Error &&
                    e.message.includes("already answered");

                if (!alreadyAnswered) {
                    dispatch({ type: "answerFailed", optionId });
                }

                throw e;
            }
        },
        setLeaving: (leaving) => {
            leavingRef.current = leaving;
        },
    };

    return (
        <GameRoomContext.Provider value={value}>
            <Outlet />
        </GameRoomContext.Provider>
    );
}
