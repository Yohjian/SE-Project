import { createContext, useContext } from "react";
import {
    GamePhase,
    type GamePhaseValue,
    type LeaderboardEntry,
    type PlayerInfo,
    type QuestionInfo,
    type QuestionResult,
    type RoomSnapshot,
} from "../api/game_types";

export interface GameRoomState {
    phase: GamePhaseValue;
    quizTitle: string;
    isHost: boolean;
    me: PlayerInfo | null;
    players: PlayerInfo[];
    totalQuestions: number;
    question: QuestionInfo | null;
    /** Local Date.now() timestamps; the server only sends durations. */
    countdownEndsAt: number | null;
    questionEndsAt: number | null;
    myAnswerOptionId: number | null;
    result: QuestionResult | null;
    leaderboard: LeaderboardEntry[];
}

export type GameRoomAction =
    | { type: "snapshot"; snapshot: RoomSnapshot }
    | { type: "playerJoined"; player: PlayerInfo }
    | { type: "playerLeft"; participantId: number }
    | { type: "gameStarted"; totalQuestions: number; startsInMs: number }
    | { type: "questionStarted"; question: QuestionInfo }
    | { type: "questionEnded"; result: QuestionResult }
    | { type: "scoreUpdated"; leaderboard: LeaderboardEntry[] }
    | { type: "gameFinished"; leaderboard: LeaderboardEntry[] }
    | { type: "cancelled" }
    | { type: "answered"; optionId: number }
    | { type: "answerFailed"; optionId: number };

export function gameRoomReducer(
    state: GameRoomState | null,
    action: GameRoomAction,
): GameRoomState | null {
    if (action.type === "snapshot") {
        const s = action.snapshot;
        const now = Date.now();
        const endsAt = s.remainingMs === null ? null : now + s.remainingMs;

        return {
            phase: s.phase,
            quizTitle: s.quizTitle,
            isHost: s.isHost,
            me: s.me,
            players: s.players,
            totalQuestions: s.totalQuestions,
            question: s.currentQuestion,
            countdownEndsAt: s.phase === GamePhase.Countdown ? endsAt : null,
            questionEndsAt: s.phase === GamePhase.QuestionOpen ? endsAt : null,
            myAnswerOptionId: s.myAnswerOptionId,
            result:
                s.phase === GamePhase.QuestionReview &&
                    s.currentQuestion &&
                    s.correctOptionId !== null
                    ? {
                        questionIndex: s.currentQuestion.index,
                        correctOptionId: s.correctOptionId,
                        optionCounts: {},
                    }
                    : null,
            leaderboard: s.leaderboard,
        };
    }

    if (!state) return state;

    switch (action.type) {
        case "playerJoined": {
            const exists = state.players.some((p) => p.id === action.player.id);
            return {
                ...state,
                players: exists
                    ? state.players.map((p) =>
                        p.id === action.player.id ? action.player : p,
                    )
                    : [...state.players, action.player],
            };
        }
        case "playerLeft":
            return {
                ...state,
                players: state.players.filter((p) => p.id !== action.participantId),
            };
        case "gameStarted":
            return {
                ...state,
                phase: GamePhase.Countdown,
                totalQuestions: action.totalQuestions,
                countdownEndsAt: Date.now() + action.startsInMs,
            };
        case "questionStarted":
            return {
                ...state,
                phase: GamePhase.QuestionOpen,
                question: action.question,
                countdownEndsAt: null,
                questionEndsAt: Date.now() + action.question.durationMs,
                myAnswerOptionId: null,
                result: null,
            };
        case "questionEnded":
            return {
                ...state,
                phase: GamePhase.QuestionReview,
                questionEndsAt: null,
                result: action.result,
            };
        case "scoreUpdated":
            return { ...state, leaderboard: action.leaderboard };
        case "gameFinished":
            return { ...state, phase: GamePhase.Finished, leaderboard: action.leaderboard };
        case "cancelled":
            return { ...state, phase: GamePhase.Cancelled };
        case "answered":
            return { ...state, myAnswerOptionId: action.optionId };
        case "answerFailed":
            return state.myAnswerOptionId === action.optionId
                ? { ...state, myAnswerOptionId: null }
                : state;
        default:
            return state;
    }
}

export interface GameRoomContextValue {
    code: string;
    state: GameRoomState;
    reconnecting: boolean;
    startGame: () => Promise<void>;
    submitAnswer: (optionId: number) => Promise<void>;
    leaveRoom: () => Promise<void>;
    /** Set before leaving on purpose, so our own GameCancelled is not shown as "room closed". */
    setLeaving: (value: boolean) => void;
}

export const GameRoomContext = createContext<GameRoomContextValue | null>(null);

export function useGameRoom(): GameRoomContextValue {
    const value = useContext(GameRoomContext);
    if (!value) throw new Error("useGameRoom must be used inside GameRoomLayout.");
    return value;
}