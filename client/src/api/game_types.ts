export const GamePhase = {
    Waiting: 0,
    Countdown: 1,
    QuestionOpen: 2,
    QuestionReview: 3,
    Finished: 4,
    Cancelled: 5,
} as const;

export type GamePhaseValue = (typeof GamePhase)[keyof typeof GamePhase];

export interface PlayerInfo {
    id: number;
    name: string;
    score: number;
    connected: boolean;
}

export interface LeaderboardEntry {
    participantId: number;
    name: string;
    score: number;
    rank: number;
    pointsGained: number;
}

export interface LiveOption {
    id: number;
    text: string;
}

export interface QuestionInfo {
    index: number;
    total: number;
    text: string;
    options: LiveOption[];
    durationMs: number;
}

export interface QuestionResult {
    questionIndex: number;
    correctOptionId: number;
    optionCounts: Record<string, number>;
}

export interface GameStartedInfo {
    totalQuestions: number;
    startsInMs: number;
}

export interface RoomSnapshot {
    joinCode: string;
    quizTitle: string;
    phase: GamePhaseValue;
    isHost: boolean;
    me: PlayerInfo | null;
    players: PlayerInfo[];
    totalQuestions: number;
    currentQuestion: QuestionInfo | null;
    remainingMs: number | null;
    myAnswerOptionId: number | null;
    correctOptionId: number | null;
    leaderboard: LeaderboardEntry[];
}