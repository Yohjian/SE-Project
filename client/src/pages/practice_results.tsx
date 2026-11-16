import { useNavigate } from "react-router-dom";

interface PracticeResultsProps {
    correctAnswers: number;
    totalQuestions: number;
}

export default function PracticeResults({
    correctAnswers,
    totalQuestions,
}: PracticeResultsProps) {
    const navigate = useNavigate();
    return (
        <div className="practice-results">
            <h2 className="page-title">Practice completed!</h2>

            <p className="play__verdict">
                {correctAnswers} / {totalQuestions} correct
            </p>

            <button
                type="button"
                className="btn btn-primary"
                onClick={() => navigate("/practice")}
            >
                Back to practice
            </button>
        </div>
    );
}