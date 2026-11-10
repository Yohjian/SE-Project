import {
    HubConnection,
    HubConnectionBuilder,
    HubConnectionState,
    LogLevel,
} from "@microsoft/signalr";
import { getToken } from "./auth";
import {
    GameStartedInfo,
    LeaderboardEntry,
    PlayerInfo,
    QuestionInfo,
    QuestionResult,
    RoomSnapshot,
} from "./game_types";

export interface GameHubHandlers {
    onSnapshot?: (snapshot: RoomSnapshot) => void; // first join and every reconnect
    onPlayerJoined?: (player: PlayerInfo) => void;
    onPlayerLeft?: (participantId: number) => void;
    onGameStarted?: (info: GameStartedInfo) => void;
    onQuestionStarted?: (question: QuestionInfo) => void;
    onQuestionEnded?: (result: QuestionResult) => void;
    onScoreUpdated?: (leaderboard: LeaderboardEntry[]) => void;
    onGameFinished?: (leaderboard: LeaderboardEntry[]) => void;
    onGameCancelled?: () => void;
    onReconnecting?: () => void;
    onReconnected?: () => void;
    onConnectionLost?: () => void; // automatic reconnect gave up, or rejoin failed
}

export interface GameHubOptions {
    joinCode: string;
    /** Host: null, identified by the JWT. Player: the stored player token. */
    playerToken: string | null;
}

const RETRY_DELAYS_MS = [0, 1000, 2000, 5000, 10000, 10000, 10000, 10000];

function hubUrl(): string {
    const base = import.meta.env.VITE_API_BASE_URL as string | undefined;
    if (base && /^https?:\/\//i.test(base)) {
        return new URL("/hubs/game", base).toString();
    }
    return "/hubs/game";
}

export function hubErrorMessage(error: unknown): string {
    const text = error instanceof Error ? error.message : String(error);
    const marker = "HubException: ";
    const at = text.lastIndexOf(marker);
    return at >= 0 ? text.slice(at + marker.length) : text;
}

export class GameHub {
    private connection: HubConnection;

    constructor(
        private readonly options: GameHubOptions,
        private readonly handlers: GameHubHandlers,
    ) {
        const isHost = options.playerToken === null;

        this.connection = new HubConnectionBuilder()
            .withUrl(hubUrl(), {
                // Players connect anonymously; only the host sends the JWT.
                ...(isHost && { accessTokenFactory: () => getToken() ?? "" }),
            })
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: (ctx) =>
                    RETRY_DELAYS_MS[ctx.previousRetryCount] ?? null,
            })
            .configureLogging(LogLevel.Warning)
            .build();

        this.registerEvents();
        this.registerLifecycle();
    }

    /** Connects, joins the room and returns the snapshot. Throws with the server's message. */
    async start(): Promise<RoomSnapshot> {
        await this.connection.start();
        try {
            const snapshot = await this.join();
            this.handlers.onSnapshot?.(snapshot);
            return snapshot;
        } catch (error) {
            await this.stop();
            throw new Error(hubErrorMessage(error));
        }
    }

    startGame = () => this.connection.invoke("StartGame");

    submitAnswer = (questionIndex: number, optionId: number) =>
        this.connection.invoke("SubmitAnswer", questionIndex, optionId);

    leaveRoom = () => this.connection.invoke("LeaveRoom");

    async stop() {
        if (this.connection.state !== HubConnectionState.Disconnected) {
            await this.connection.stop();
        }
    }

    private join() {
        return this.connection.invoke<RoomSnapshot>(
            "JoinRoom",
            this.options.joinCode,
            this.options.playerToken,
        );
    }

    private registerEvents() {
        const c = this.connection;
        const h = this.handlers;

        c.on("PlayerJoined", (p: PlayerInfo) => h.onPlayerJoined?.(p));
        c.on("PlayerLeft", (e: { participantId: number }) =>
            h.onPlayerLeft?.(e.participantId));
        c.on("GameStarted", (e: GameStartedInfo) => h.onGameStarted?.(e));
        c.on("QuestionStarted", (q: QuestionInfo) => h.onQuestionStarted?.(q));
        c.on("QuestionEnded", (r: QuestionResult) => h.onQuestionEnded?.(r));
        c.on("ScoreUpdated", (e: { leaderboard: LeaderboardEntry[] }) =>
            h.onScoreUpdated?.(e.leaderboard));
        c.on("GameFinished", (e: { leaderboard: LeaderboardEntry[] }) =>
            h.onGameFinished?.(e.leaderboard));
        c.on("GameCancelled", () => h.onGameCancelled?.());
    }

    private registerLifecycle() {
        const c = this.connection;

        c.onreconnecting(() => this.handlers.onReconnecting?.());

        c.onreconnected(async () => {
            // A new connection has no room: join again and replace local state with the snapshot.
            try {
                const snapshot = await this.join();
                this.handlers.onSnapshot?.(snapshot);
                this.handlers.onReconnected?.();
            } catch {
                this.handlers.onConnectionLost?.();
            }
        });

        c.onclose(() => this.handlers.onConnectionLost?.());
    }
}