import { useEffect, useState } from "react";
import { practiceApi } from "../api/client";
import type { PracticeQuizSummary } from "../api/types";
import { useNavigate } from "react-router-dom";
import "../styles/quizzes.css";

export default function Practice() {
    const navigate = useNavigate();
    const [quizzes, setQuizzes] = useState<PracticeQuizSummary[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        practiceApi.getQuizzes().then((response) => {
            setQuizzes(response.data);
        })
            .catch((error) => {
                console.error("Error fetching quizzes:", error);
                setError("Failed to load quizzes.");
            })
            .finally(() => {
                setLoading(false);
            });
    }, []);

    return (
        <div className="container quizzes-page practice-page">
            <div className="quizzes-header">
                <h1 className="page-title">Practice Mode</h1>
            </div>

            <p className="text-muted">
                Choose a quiz to start practicing.
            </p>

            {loading && (<p className="text-muted">Loading quizzes...</p>)}

            {!loading && error && (<p>{error}</p>)}

            {!loading && !error && quizzes.length === 0 && (
                <div className="quizzes-empty">
                    <h2>No practice quizzes available</h2>
                    <p className="text-muted">
                        Create a practice quiz to get started.
                    </p>
                    <button
                        type="button"
                        className="btn btn-primary"
                        onClick={() => navigate("/quizzes")}
                    >
                        Go to quizzes
                    </button>
                </div>
            )}

            {!loading && !error && quizzes.length > 0 && (
                <ul className="quiz-grid">
                    {quizzes.map((quiz) => (
                        <li key={quiz.id} className="quiz-card">
                            <h2 className="quiz-card-title">
                                {quiz.title}
                            </h2>

                            {quiz.description && (
                                <p className="quiz-card-description">
                                    {quiz.description}
                                </p>
                            )}

                            <p className="text-muted">
                                Questions: {quiz.questionCount}
                            </p>

                            <button
                                type="button"
                                className="btn btn-primary"
                                onClick={() =>
                                    navigate(`/practice/${quiz.id}`)
                                }
                                disabled={quiz.questionCount === 0}
                            >
                                Start practice
                            </button>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}
