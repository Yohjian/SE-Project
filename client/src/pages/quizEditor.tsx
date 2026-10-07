import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { quizApi } from "../api/client";
import { QuestionRequest } from "../api/types";
import { useNotification } from "../components/notification";

import "../styles/quizEditor.css";

export default function QuizEditor() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [questions, setQuestions] = useState<QuestionRequest[]>([]);
  const [isSaving, setIsSaving] = useState(false);

  const { showNotification } = useNotification();

  useEffect(() => {
    if (!id) return;

    quizApi
      .getById(Number(id))
      .then((res) => {
        setTitle(res.data.title);
        setDescription(res.data.description ?? "");

        setQuestions(
          res.data.questions.map((question) => ({
            text: question.text,
            timeLimitSeconds: question.timeLimitSeconds,
            points: question.points,
            orderIndex: question.orderIndex,
            answerOptions: question.answerOptions.map((answer) => ({
              text: answer.text,
              isCorrect: answer.isCorrect,
            })),
          })),
        );
      })
      .catch(() => showNotification("Failed to load quiz."));
  }, [id]);

  const handleAddQuestion = () => {
    setQuestions((prev) => [
      ...prev,
      {
        text: "",
        timeLimitSeconds: 30,
        points: 100,
        orderIndex: prev.length,
        answerOptions: [
          { text: "", isCorrect: true },
          { text: "", isCorrect: false },
        ],
      },
    ]);
  };

  const handleQuestionChange = (
    questionIndex: number,
    field: "text" | "timeLimitSeconds" | "points",
    value: string | number,
  ) => {
    setQuestions((prev) =>
      prev.map((question, index) =>
        index === questionIndex
          ? { ...question, [field]: value }
          : question,
      ),
    );
  };

  const handleDeleteQuestion = (questionIndex: number) => {
    setQuestions((prev) =>
      prev
        .filter((_, index) => index !== questionIndex)
        .map((question, index) => ({
          ...question,
          orderIndex: index,
        })),
    );
  };

  const handleAddAnswer = (questionIndex: number) => {
    setQuestions((prev) =>
      prev.map((question, index) =>
        index === questionIndex
          ? {
              ...question,
              answerOptions: [
                ...question.answerOptions,
                { text: "", isCorrect: false },
              ],
            }
          : question,
      ),
    );
  };

  const handleAnswerChange = (
    questionIndex: number,
    answerIndex: number,
    value: string,
  ) => {
    setQuestions((prev) =>
      prev.map((question, index) =>
        index === questionIndex
          ? {
              ...question,
              answerOptions: question.answerOptions.map((answer, index) =>
                index === answerIndex
                  ? { ...answer, text: value }
                  : answer,
              ),
            }
          : question,
      ),
    );
  };

  const handleCorrectAnswer = (
    questionIndex: number,
    answerIndex: number,
  ) => {
    setQuestions((prev) =>
      prev.map((question, index) =>
        index === questionIndex
          ? {
              ...question,
              answerOptions: question.answerOptions.map((answer, index) => ({
                ...answer,
                isCorrect: index === answerIndex,
              })),
            }
          : question,
      ),
    );
  };

  const handleDeleteAnswer = (
    questionIndex: number,
    answerIndex: number,
  ) => {
    setQuestions((prev) =>
      prev.map((question, index) =>
        index === questionIndex
          ? {
              ...question,
              answerOptions: question.answerOptions.filter(
                (_, index) => index !== answerIndex,
              ),
            }
          : question,
      ),
    );
  };

  const handleSave = async () => {
    if (!id) return;

    if (!title.trim()) {
      showNotification("A title is needed!");
      return;
    }

    setIsSaving(true);

    try {
      await quizApi.update(Number(id), {
        title,
        description: description || null,
        questions,
      });

      showNotification("Quiz saved.", "success");
    } catch {
      showNotification("Failed to save quiz.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="quiz-editor-page">
      <div className="quiz-editor-header">
        <h1>Edit Quiz</h1>

        <button onClick={() => navigate("/quizzes")}>
          Back
        </button>
      </div>

      <div className="quiz-editor-form">
        <div className="quiz-title-section">
          <label>Title</label>

          <div className="title-input-wrapper">
            <span>"</span>

            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
            />

            <span>"</span>
          </div>
        </div>

        <div className="quiz-description-section">
          <label>Description</label>

          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Quiz description"
          />
        </div>

        <div className="questions-section">
          <h2>Questions</h2>

          {questions.map((question, questionIndex) => (
            <div className="question-card" key={questionIndex}>
              <div className="question-header">
                <h3>Question {questionIndex + 1}</h3>

                <button
                  type="button"
                  onClick={() => handleDeleteQuestion(questionIndex)}
                >
                  Delete question
                </button>
              </div>

              <input
                type="text"
                placeholder="Question text"
                value={question.text}
                onChange={(e) =>
                  handleQuestionChange(
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
                      handleQuestionChange(
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
                      handleQuestionChange(
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
                      handleCorrectAnswer(
                        questionIndex,
                        answerIndex,
                      )
                    }
                  />

                  <input
                    type="text"
                    placeholder={`Answer ${answerIndex + 1}`}
                    value={answer.text}
                    onChange={(e) =>
                      handleAnswerChange(
                        questionIndex,
                        answerIndex,
                        e.target.value,
                      )
                    }
                  />

                  <button
                    type="button"
                    onClick={() =>
                      handleDeleteAnswer(
                        questionIndex,
                        answerIndex,
                      )
                    }
                  >
                    Delete
                  </button>
                </div>
              ))}

              <button
                type="button"
                onClick={() => handleAddAnswer(questionIndex)}
              >
                + Add answer
              </button>
            </div>
          ))}

          <button
            className="add-question-button"
            type="button"
            onClick={handleAddQuestion}
          >
            + Add question
          </button>
        </div>

        <div className="save-section">
          <button
            className="save-button"
            onClick={handleSave}
            disabled={isSaving}
          >
            {isSaving ? "Saving..." : "Save"}
          </button>
        </div>
      </div>
    </div>
  );
}