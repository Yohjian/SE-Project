import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { practiceApi } from "../api/client";
import type { PracticeQuizResponse, PracticeAnswerResponse, } from "../api/types";
import "../styles/game.css";
import PracticeResults from "../pages/practice_results";

export default function PracticePlay() {
    const { id } = useParams<{ id: string }>();
    const [quiz, setQuiz] = useState<PracticeQuizResponse | null>(null);
    const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);
    const currentQuestion = quiz?.questions[currentQuestionIndex];
    const [selectedAnswerId, setSelectedAnswerId] = useState<number | null>(null);
    const [answerResult, setAnswerResult] = useState<PracticeAnswerResponse | null>(null);
    const [isChecking, setIsChecking] = useState(false);
    const [correctAnswers, setCorrectAnswers] = useState(0);
    const [isFinished, setIsFinished] = useState(false);

    useEffect(() => {
        if (!id) return;

        practiceApi
            .getQuiz(Number(id))
            .then((response) => {
                setQuiz(response.data);
            })
            .catch((error) => {
                console.error("Error fetching practice quiz:", error);
            });
    }, [id]);

    const handleCheckAnswer = async () => {
        if (!quiz || !currentQuestion || selectedAnswerId === null || isChecking || answerResult !== null) {
            return;
        }

        setIsChecking(true);

        try {
            const response = await practiceApi.checkAnswer({
                quizId: quiz.id,
                questionId: currentQuestion.id,
                answerOptionId: selectedAnswerId,
            });

            setAnswerResult(response.data);
            if (response.data.isCorrect) {
                setCorrectAnswers((prev) => prev + 1);
            }
        } catch (error) {
            console.error("Failed to check practice answer:", error);
        } finally {
            setIsChecking(false);
        }
    };



    const handleNextQuestion = () => {
        if (!quiz || !answerResult) {
            return;
        }

        if (currentQuestionIndex === quiz.questions.length - 1) {
            setIsFinished(true);
            return;
        }

        setCurrentQuestionIndex((prev) => prev + 1);
        setSelectedAnswerId(null);
        setAnswerResult(null);
    };



    return (
        <div className="container play">
            {quiz && (
                <>
                    <h1 className="page-title">{quiz.title}</h1>

                    {isFinished ? (
                        <PracticeResults
                            correctAnswers={correctAnswers}
                            totalQuestions={quiz.questions.length}
                        />
                    ) : currentQuestion ? (
                        <>
                            <div className="play__meta">
                                <span>
                                    Question {currentQuestionIndex + 1} /{" "}
                                    {quiz.questions.length}
                                </span>
                            </div>


                            <h2 className="play__question">
                                {currentQuestion.text}
                            </h2>
                            <ul className="play__options">
                                {currentQuestion.answerOptions.map((option) => {
                                    const isSelected = selectedAnswerId === option.id;

                                    const classes = ["play-option"];

                                    if (isSelected) {
                                        classes.push("play-option--picked");
                                    }

                                    if (answerResult) {
                                        if (option.id === answerResult.correctAnswerOptionId) {
                                            classes.push("play-option--correct");
                                        } else if (isSelected) {
                                            classes.push("play-option--wrong");
                                        }
                                    }

                                    return (
                                        <li key={option.id}>
                                            <button
                                                type="button"
                                                className={classes.join(" ")}
                                                disabled={answerResult !== null || isChecking}
                                                aria-pressed={isSelected}
                                                onClick={() => setSelectedAnswerId(option.id)}
                                            >
                                                <span>{option.text}</span>
                                            </button>
                                        </li>
                                    );
                                })}
                            </ul>

                            {answerResult === null && (
                                <button
                                    type="button"
                                    className="btn btn-primary"
                                    disabled={selectedAnswerId === null || isChecking}
                                    onClick={() => void handleCheckAnswer()}
                                >
                                    {isChecking ? "Checking..." : "Check answer"}
                                </button>
                            )}

                            {answerResult && (
                                <p className="play__verdict">
                                    {answerResult.isCorrect ? "Correct!" : "Wrong!"}
                                </p>
                            )}



                            {answerResult && (
                                <button
                                    type="button"
                                    className="btn btn-primary"
                                    onClick={handleNextQuestion}
                                >
                                    {currentQuestionIndex === quiz.questions.length - 1
                                        ? "See results"
                                        : "Next question"}
                                </button>
                            )}

                        </>
                    ) : (
                        <p className="play__notice text-muted">
                            No questions available.
                        </p>
                    )}
                </>
            )}
        </div>
    );

}
