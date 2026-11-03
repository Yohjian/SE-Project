const key = (code: string) => `player_session_${code}`;

export interface PlayerSession {
    participantId: number;
    displayName: string;
    playerToken: string;
}

export function savePlayerSession(code: string, session: PlayerSession) {
    sessionStorage.setItem(key(code), JSON.stringify(session));
}

export function getPlayerSession(code: string): PlayerSession | null {
    const raw = sessionStorage.getItem(key(code));
    return raw ? (JSON.parse(raw) as PlayerSession) : null;
}

export function clearPlayerSession(code: string) {
    sessionStorage.removeItem(key(code));
}