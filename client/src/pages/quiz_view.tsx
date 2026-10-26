import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { quizApi } from "../api/client";
import type { QuizEditorResponse } from "../api/types";

import "../styles/quiz_view.css";

export default function QuizView() {
    const { id } = useParams();

    const [quiz, setQuiz] = useState<QuizEditorResponse | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const quizId = Number(id);

        if (!id || !Number.isSafeInteger(quizId) || quizId <= 0) {
            setError("Invalid quiz.");
            setIsLoading(false);
            return;
        }

        quizApi
            .getPublicById(quizId)
            .then((response) => {
                setQuiz(response.data);
            })
            .catch(() => {
                setError("Quiz not found or is private.");
            })
            .finally(() => {
                setIsLoading(false);
            });
    }, [id]);

    if (isLoading) {
        return (
            <div className="container quiz-view">
                <p className="text-muted">Loading quiz...</p>
            </div>
        );
    }

    if (error || !quiz) {
        return (
            <div className="container quiz-view">
                <div className="alert-error" role="alert">
                    {error || "Quiz not found."}
                </div>

                <Link to="/dashboard" className="btn btn-secondary">
                    Back to dashboard
                </Link>
            </div>
        );
    }

    const questions = [...quiz.questions].sort(
        (a, b) => a.orderIndex - b.orderIndex
    );

    return (
        <div className="container quiz-view">
            <header className="quiz-view-header">
                <div>
                    <h1 className="page-title">{quiz.title}</h1>

                    {quiz.description && (
                        <p className="text-muted">{quiz.description}</p>
                    )}

                    <p className="text-muted">
                        {questions.length}{" "}
                        {questions.length === 1 ? "question" : "questions"}
                    </p>
                </div>

                <Link to="/dashboard" className="btn btn-secondary">
                    Back
                </Link>
            </header>

            {questions.length === 0 ? (
                <div className="questions-empty">
                    <p>This quiz has no questions yet.</p>
                </div>
            ) : (
                <div className="quiz-view-questions">
                    {questions.map((question, index) => (
                        <section
                            key={question.id}
                            className="question-card"
                        >
                            <div className="quiz-view-question-header">
                                <span className="question-number">
                                    {index + 1}
                                </span>

                                <h2>{question.text}</h2>
                            </div>

                            <div className="question-details">
                                <div className="quiz-view-settings">
                                    <span className="question-pill">
                                        {question.timeLimitSeconds} seconds
                                    </span>

                                    <span className="question-pill">
                                        {question.points} points
                                    </span>
                                </div>

                                <div className="quiz-view-answers">
                                    {question.answerOptions.map(
                                        (answer, answerIndex) => (
                                            <div
                                                key={answer.id}
                                                className={
                                                    answer.isCorrect
                                                        ? "quiz-view-answer is-correct"
                                                        : "quiz-view-answer"
                                                }
                                            >
                                                <span>
                                                    {String.fromCharCode(
                                                        65 + answerIndex
                                                    )}
                                                    .
                                                </span>

                                                <span className="quiz-view-answer-text">
                                                    {answer.text}
                                                </span>

                                                {answer.isCorrect && (
                                                    <span className="quiz-view-correct">
                                                        Correct
                                                    </span>
                                                )}
                                            </div>
                                        )
                                    )}
                                </div>
                            </div>
                        </section>
                    ))}
                </div>
            )}
        </div>
    );
}