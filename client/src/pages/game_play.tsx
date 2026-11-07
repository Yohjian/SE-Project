import { useState } from "react";
import { GamePhase } from "../api/game_types";
import Leaderboard from "../components/leaderboard";
import { useGameRoom } from "../components/game_room_state";
import { useRemainingMs } from "../components/use_remaining_ms";
import "../styles/game.css";

export default function GamePlay() {
    const { state, reconnecting, submitAnswer } = useGameRoom();
    const [error, setError] = useState("");

    const questionMs = useRemainingMs(state.questionEndsAt);
    const countdownMs = useRemainingMs(state.countdownEndsAt);

    const question = state.question;
    const result = state.phase === GamePhase.QuestionReview ? state.result : null;
    const hasCounts = !!result && Object.keys(result.optionCounts).length > 0;
    const meId = state.me?.id;

    const canAnswer =
        !state.isHost &&
        state.phase === GamePhase.QuestionOpen &&
        state.myAnswerOptionId === null &&
        questionMs > 0;

    const handleAnswer = async (optionId: number) => {
        setError("");
        try {
            await submitAnswer(optionId);
        } catch (e) {
            setError(e instanceof Error ? e.message : "Could not send your answer.");
        }
    };

    const banner = reconnecting && (
        <div className="alert-error" role="status">
            Reconnecting…
        </div>
    );

    if (state.phase === GamePhase.Countdown || !question) {
        return (
            <div className="container play">
                {banner}
                <p className="play__notice text-muted">Get ready!</p>
                <p className="play__countdown">{Math.ceil(countdownMs / 1000)}</p>
            </div>
        );
    }

    const verdict =
        result && !state.isHost
            ? state.myAnswerOptionId === null
                ? "Time's up!"
                : state.myAnswerOptionId === result.correctOptionId
                  ? "Correct!"
                  : "Wrong"
            : null;

    const myEntry = state.leaderboard.find((e) => e.participantId === meId);
    const gained =
        verdict === "Correct!" && myEntry && myEntry.pointsGained > 0
            ? ` +${myEntry.pointsGained}`
            : "";

    return (
        <div className="container play">
            {banner}

            <div className="play__meta">
                <span>
                    Question {question.index + 1} / {question.total}
                </span>
                {state.isHost && <span>Hosting: watching</span>}
            </div>

            {state.phase === GamePhase.QuestionOpen && (
                <div className="play__timer">
                    <div
                        className="play__timer-fill"
                        style={{ width: `${Math.min(100, (questionMs / question.durationMs) * 100)}%` }}
                    />
                </div>
            )}

            <h1 className="play__question">{question.text}</h1>

            <ul className="play__options">
                {question.options.map((o) => {
                    const picked = state.myAnswerOptionId === o.id;
                    const classes = ["play-option"];
                    if (picked) classes.push("play-option--picked");
                    if (result) {
                        if (o.id === result.correctOptionId) classes.push("play-option--correct");
                        else if (picked) classes.push("play-option--wrong");
                    }

                    return (
                        <li key={o.id}>
                            <button
                                type="button"
                                className={classes.join(" ")}
                                disabled={!canAnswer}
                                onClick={() => void handleAnswer(o.id)}
                            >
                                <span>{o.text}</span>
                                {hasCounts && (
                                    <span className="play-option__count">
                                        {result.optionCounts[String(o.id)] ?? 0}
                                    </span>
                                )}
                            </button>
                        </li>
                    );
                })}
            </ul>

            {error && (
                <div className="alert-error" role="alert">
                    {error}
                </div>
            )}

            {state.phase === GamePhase.QuestionOpen && state.myAnswerOptionId !== null && (
                <p className="play__notice text-muted">Answer locked. Waiting for the others…</p>
            )}

            {verdict && <p className="play__verdict">{verdict + gained}</p>}

            {result && (
                <Leaderboard entries={state.leaderboard} meId={meId} limit={5} showGain />
            )}
        </div>
    );
}