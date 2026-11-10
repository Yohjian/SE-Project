import { useEffect, useState } from "react";
import { practiceApi } from "../api/client";
import type { PracticeQuizSummary } from "../api/types";
import { useNavigate } from "react-router-dom";

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
        <main>
            <h1>Practice Mode</h1>
            <p>Choose a quiz to start practicing.</p>

            {loading && <p>Loading quizzes...</p>}

            {!loading && error && <p>{error}</p>}

            {!loading && !error && quizzes.length === 0 && (
                <p>No practice quizzes available.</p>
            )}

            {!loading &&
                quizzes.map((quiz) => (
                    <div key={quiz.id}>
                        <h2>{quiz.title}</h2>
                        <p>{quiz.description}</p>
                        <p>Questions: {quiz.questionCount}</p>

                        <button
                            onClick={() => navigate(`/practice/${quiz.id}`)}
                        >
                            Start Practice
                        </button>
                    </div>
                ))}
        </main>
    );
}
