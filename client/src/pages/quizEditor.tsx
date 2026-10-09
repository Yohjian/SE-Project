import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { quizApi } from "../api/client";
import { QuestionRequest } from "../api/types";
import { useNotification } from "../components/notification";
import QuizQuestionEditor from "../components/quiz_question_editor";

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

  const handleMoveQuestion = (
  questionIndex: number,
  direction: "up" | "down",
) => {
  setQuestions((prev) => {
    const newQuestions = [...prev];

    const newIndex =
      direction === "up"
        ? questionIndex - 1
        : questionIndex + 1;

    if (newIndex < 0 || newIndex >= newQuestions.length) {
      return prev;
    }

    [newQuestions[questionIndex], newQuestions[newIndex]] = [
      newQuestions[newIndex],
      newQuestions[questionIndex],
    ];

    return newQuestions.map((question, index) => ({
      ...question,
      orderIndex: index,
    }));
  });
};

  const handleAddAnswer = (questionIndex: number) => {
    setQuestions((prev) =>
      prev.map((question, index) =>
        index === questionIndex
          ? {
              ...question,
              answerOptions: [
                ...question.answerOptions,
                {
                  text: "",
                  isCorrect: false,
                },
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
              answerOptions: question.answerOptions.map(
                (answer, index) =>
                  index === answerIndex
                    ? {
                        ...answer,
                        text: value,
                      }
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
              answerOptions: question.answerOptions.map(
                (answer, index) => ({
                  ...answer,
                  isCorrect: index === answerIndex,
                }),
              ),
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
              answerOptions:
                question.answerOptions.filter(
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

    if (questions.some((question) => !question.text.trim())) {
      showNotification("Every question needs text.");
      return;
    }

    if (
      questions.some((question) =>
        question.answerOptions.some((answer) => !answer.text.trim()),
      )
    ) {
      showNotification("Every answer needs text.");
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

            <input
              type="text"
              value={title}
              size={title.length || 1}
              onChange={(e) => setTitle(e.target.value)}
            />

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
            <QuizQuestionEditor
              key={questionIndex}
              question={question}
              questionIndex={questionIndex}
              questionCount={questions.length}
              onQuestionChange={handleQuestionChange}
              onDeleteQuestion={handleDeleteQuestion}
              onAddAnswer={handleAddAnswer}
              onAnswerChange={handleAnswerChange}
              onCorrectAnswer={handleCorrectAnswer}
              onDeleteAnswer={handleDeleteAnswer}
              onMoveQuestion={handleMoveQuestion}
            />
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