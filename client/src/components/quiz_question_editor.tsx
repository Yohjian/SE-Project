import { useId, useState } from "react";
import type { QuestionRequest } from "../api/types";

interface QuizQuestionEditorProps {
    question: QuestionRequest;
    questionIndex: number;
    questionCount: number;

    onQuestionChange: (
        questionIndex: number,
        field: "text" | "timeLimitSeconds" | "points",
        value: string | number,
    ) => void;

    onDeleteQuestion: (questionIndex: number) => void;
    onMoveQuestion: (questionIndex: number, direction: "up" | "down") => void;
    onAddAnswer: (questionIndex: number) => void;

    onAnswerChange: (
        questionIndex: number,
        answerIndex: number,
        value: string,
    ) => void;

    onCorrectAnswer: (questionIndex: number, answerIndex: number) => void;

    onDeleteAnswer: (questionIndex: number, answerIndex: number) => void;
}

const iconProps = {
    width: 16,
    height: 16,
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 2,
    strokeLinecap: "round",
    strokeLinejoin: "round",
    "aria-hidden": true,
} as const;

export default function QuizQuestionEditor({
    question,
    questionIndex,
    questionCount,
    onQuestionChange,
    onDeleteQuestion,
    onAddAnswer,
    onAnswerChange,
    onCorrectAnswer,
    onDeleteAnswer,
    onMoveQuestion,
}: QuizQuestionEditorProps) {
    const [isOpen, setIsOpen] = useState(false);

    const baseId = useId();
    const panelId = `${baseId}-panel`;
    const textId = `${baseId}-text`;
    const timeId = `${baseId}-time`;
    const pointsId = `${baseId}-points`;

    const words = question.text.trim().split(/\s+/).filter(Boolean);
    const preview =
        words.length > 0
            ? words.slice(0, 3).join(" ") + (words.length > 3 ? "…" : "")
            : "Untitled question";

    return (
        <div className="question-card">
            <div className="question-header">
                <div className="question-move">
                    <button
                        className="editor-icon-btn"
                        type="button"
                        aria-label="Move question up"
                        title="Move up"
                        disabled={questionIndex === 0}
                        onClick={() => onMoveQuestion(questionIndex, "up")}
                    >
                        <svg {...iconProps}>
                            <path d="M6 15l6-6 6 6" />
                        </svg>
                    </button>

                    <button
                        className="editor-icon-btn"
                        type="button"
                        aria-label="Move question down"
                        title="Move down"
                        disabled={questionIndex === questionCount - 1}
                        onClick={() => onMoveQuestion(questionIndex, "down")}
                    >
                        <svg {...iconProps}>
                            <path d="M6 9l6 6 6-6" />
                        </svg>
                    </button>
                </div>

                <button
                    type="button"
                    className="question-toggle"
                    aria-expanded={isOpen}
                    aria-controls={panelId}
                    onClick={() => setIsOpen(!isOpen)}
                >
                    <span className="question-number">{questionIndex + 1}</span>

                    <span
                        className={
                            words.length > 0
                                ? "question-preview"
                                : "question-preview is-empty"
                        }
                    >
                        {preview}
                    </span>

                    {question.timeLimitSeconds > 0 && (
                        <span className="question-pill">
                            {question.timeLimitSeconds} s
                        </span>
                    )}
                    {question.points > 0 && (
                        <span className="question-pill">
                            {question.points} pts
                        </span>
                    )}

                    <svg className="question-chevron" {...iconProps}>
                        <path d="M6 9l6 6 6-6" />
                    </svg>
                </button>

                <button
                    className="editor-icon-btn editor-icon-btn-danger"
                    aria-label={`Delete question ${questionIndex + 1}`}
                    title="Delete question"
                    type="button"
                    onClick={() => onDeleteQuestion(questionIndex)}
                >
                    <svg {...iconProps}>
                        <path d="M3 6h18" />
                        <path d="M8 6V4h8v2" />
                        <path d="M19 6l-1 14H6L5 6" />
                        <path d="M10 11v6M14 11v6" />
                    </svg>
                </button>
            </div>

            {isOpen && (
                <div className="question-details" id={panelId}>
                    <div className="field">
                        <label htmlFor={textId}>Question</label>
                        <input
                            id={textId}
                            className="input"
                            type="text"
                            placeholder="Question text"
                            value={question.text}
                            onChange={(e) =>
                                onQuestionChange(
                                    questionIndex,
                                    "text",
                                    e.target.value,
                                )
                            }
                        />
                    </div>

                    <div className="question-settings">
                        <div className="field">
                            <label htmlFor={timeId}>Time limit (seconds)</label>
                            <input
                                id={timeId}
                                className="input"
                                type="number"
                                min="5"
                                value={
                                    question.timeLimitSeconds === 0
                                        ? ""
                                        : question.timeLimitSeconds
                                }
                                onChange={(e) => {
                                    const value = e.target.value;

                                    onQuestionChange(
                                        questionIndex,
                                        "timeLimitSeconds",
                                        value === "" ? 0 : Number(value),
                                    );
                                }}
                            />
                        </div>

                        <div className="field">
                            <label htmlFor={pointsId}>Points</label>
                            <input
                                id={pointsId}
                                className="input"
                                type="number"
                                min="1"
                                value={
                                    question.points === 0 ? "" : question.points
                                }
                                onChange={(e) =>
                                    onQuestionChange(
                                        questionIndex,
                                        "points",
                                        e.target.value === ""
                                            ? 0
                                            : Number(e.target.value),
                                    )
                                }
                            />
                        </div>
                    </div>

                    <fieldset className="answers">
                        <legend>Answers</legend>
                        <p className="field-hint answers-hint">
                            Select the circle next to the correct answer.
                        </p>

                        {question.answerOptions.map((answer, answerIndex) => (
                            <div
                                className={
                                    answer.isCorrect
                                        ? "answer-row is-correct"
                                        : "answer-row"
                                }
                                key={answerIndex}
                            >
                                <input
                                    className="answer-radio"
                                    type="radio"
                                    name={`correct-answer-${baseId}`}
                                    aria-label={`Mark answer ${answerIndex + 1} as correct`}
                                    checked={answer.isCorrect}
                                    onChange={() =>
                                        onCorrectAnswer(
                                            questionIndex,
                                            answerIndex,
                                        )
                                    }
                                />

                                <input
                                    className="input"
                                    type="text"
                                    aria-label={`Answer ${answerIndex + 1}`}
                                    placeholder={`Answer ${answerIndex + 1}`}
                                    value={answer.text}
                                    onChange={(e) =>
                                        onAnswerChange(
                                            questionIndex,
                                            answerIndex,
                                            e.target.value,
                                        )
                                    }
                                />

                                <button
                                    className="editor-icon-btn editor-icon-btn-danger"
                                    aria-label={`Delete answer ${answerIndex + 1}`}
                                    title="Delete answer"
                                    type="button"
                                    onClick={() =>
                                        onDeleteAnswer(
                                            questionIndex,
                                            answerIndex,
                                        )
                                    }
                                >
                                    <svg {...iconProps}>
                                        <path d="M18 6L6 18M6 6l12 12" />
                                    </svg>
                                </button>
                            </div>
                        ))}
                    </fieldset>

                    <button
                        className="btn btn-secondary btn-sm"
                        type="button"
                        onClick={() => onAddAnswer(questionIndex)}
                    >
                        + Add answer
                    </button>
                </div>
            )}
        </div>
    );
}
