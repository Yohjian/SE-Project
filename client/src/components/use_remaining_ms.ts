import { useEffect, useState } from "react";

/** Milliseconds left until a local Date.now() timestamp; 0 when there is none. */
export function useRemainingMs(endsAt: number | null): number {
    const [now, setNow] = useState(() => Date.now());

    useEffect(() => {
        if (endsAt === null) return;
        const id = setInterval(() => setNow(Date.now()), 100);
        return () => clearInterval(id);
    }, [endsAt]);

    return endsAt === null ? 0 : Math.max(0, endsAt - now);
}