import { useNavigate } from "react-router-dom";
import { clearPlayerSession } from "../api/player_session";
import Leaderboard from "../components/leaderboard";
import { useGameRoom } from "../components/game_room_state";
import "../styles/game.css";

export default function GameResults() {
    const { code, state } = useGameRoom();
    const navigate = useNavigate();

    // Clear on click, not on mount: the layout reads the session on every render.
    const handleBack = () => {
        clearPlayerSession(code);
        navigate(state.isHost ? "/rooms" : "/");
    };

    return (
        <div className="container play">
            <h1 className="page-title">Final results</h1>
            <p className="text-muted">{state.quizTitle}</p>

            <Leaderboard entries={state.leaderboard} meId={state.me?.id} />

            <button type="button" className="btn btn-primary btn-block" onClick={handleBack}>
                {state.isHost ? "Back to my rooms" : "Back to home"}
            </button>
        </div>
    );
}