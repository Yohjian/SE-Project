import type { LeaderboardEntry } from "../api/game_types";

interface Props {
    entries: LeaderboardEntry[];
    meId?: number;
    limit?: number;
    showGain?: boolean;
}

export default function Leaderboard({ entries, meId, limit, showGain }: Props) {
    const shown = limit ? entries.slice(0, limit) : entries;

    return (
        <ol className="board">
            {shown.map((e) => (
                <li
                    key={e.participantId}
                    className={`board__row${e.participantId === meId ? " board__row--me" : ""}`}
                >
                    <span className="board__rank">{e.rank}</span>
                    <span className="board__name">{e.name}</span>
                    <span>{e.score}</span>
                    {showGain && e.pointsGained > 0 && (
                        <span className="board__gain">+{e.pointsGained}</span>
                    )}
                </li>
            ))}
        </ol>
    );
}