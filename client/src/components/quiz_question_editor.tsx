import { useState } from "react";
import { QuestionRequest } from "../api/types";

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
  onMoveQuestion: (
    questionIndex: number,
    direction: "up" | "down",
  ) => void;
  onAddAnswer: (questionIndex: number) => void;

  onAnswerChange: (
    questionIndex: number,
    answerIndex: number,
    value: string,
  ) => void;

  onCorrectAnswer: (
    questionIndex: number,
    answerIndex: number,
  ) => void;

  onDeleteAnswer: (
    questionIndex: number,
    answerIndex: number,
  ) => void;
}

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
  return (
    <div className="question-card">
      <div
        className="question-header"
        onClick={() => setIsOpen(!isOpen)}
      >
        <div className="question-header-left">
          <button
            className="move-question-button"
            type="button"
            onClick={(e) => {
              e.stopPropagation();
              onMoveQuestion(questionIndex, "up");
            }}
          >
            ↑
          </button>

          <button
            className="move-question-button"
            type="button"
            onClick={(e) => {
              e.stopPropagation();
              onMoveQuestion(questionIndex, "down");
            }}
          >
            ↓
          </button>

          <h3>
            {question.text.trim()
              ? question.text.trim().split(/\s+/).slice(0, 3).join(" ") +
                (question.text.trim().split(/\s+/).length > 3 ? "..." : "")
              : `Question ${questionIndex + 1}`}
          </h3>
        </div>

        <div className="question-header-right">
          <button
            className="delete-question-button"
            type="button"
            onClick={(e) => {
              e.stopPropagation();
              onDeleteQuestion(questionIndex);
            }}
          >
            Delete question
          </button>

          <span className="question-toggle">
            {isOpen ? "▲" : "▼"}
          </span>
        </div>
      </div>
    {isOpen && (
      <div className="question-details">
        <input
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

        <div className="question-settings">
          <div>
            <label>Time limit</label>

            <input
              type="number"
              min="1"
              value={question.timeLimitSeconds}
              onChange={(e) =>
                onQuestionChange(
                  questionIndex,
                  "timeLimitSeconds",
                  Number(e.target.value),
                )
              }
            />
          </div>

          <div>
            <label>Points</label>

            <input
              type="number"
              min="1"
              value={question.points}
              onChange={(e) =>
                onQuestionChange(
                  questionIndex,
                  "points",
                  Number(e.target.value),
                )
              }
            />
          </div>
        </div>

        <h4>Answers</h4>

        {question.answerOptions.map((answer, answerIndex) => (
          <div className="answer-row" key={answerIndex}>
            <input
              type="radio"
              name={`correct-answer-${questionIndex}`}
              checked={answer.isCorrect}
              onChange={() =>
                onCorrectAnswer(questionIndex, answerIndex)
              }
            />

            <input
              type="text"
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
              type="button"
              onClick={() =>
                onDeleteAnswer(questionIndex, answerIndex)
              }
            >
              Delete
            </button>
          </div>
        ))}
        <div className="question-actions">
          <button
            type="button"
            onClick={() => onAddAnswer(questionIndex)}
          >
            + Add answer
          </button>
        </div>
      </div>
      )}
  </div>
);
}