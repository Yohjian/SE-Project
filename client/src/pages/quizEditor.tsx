import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { quizApi } from "../api/client";
import { useNotification } from "../components/notification";

import "../styles/quizEditor.css";

export default function QuizEditor() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [isSaving, setIsSaving] = useState(false);

  const { showNotification } = useNotification();

  useEffect(() => {
    if (!id) return;

    quizApi
      .getById(Number(id))
      .then((res) => {
        setTitle(res.data.title);
        setDescription(res.data.description ?? "");
      })
      .catch(() => showNotification("Failed to load quiz."));
  }, [id]);

  const handleSave = async () => {
    if (!id) return;

    if (!title.trim()) {
      showNotification("A title is needed!");
      return;
    }

    setIsSaving(true);

    try {
      const currentQuiz = await quizApi.getById(Number(id));

      await quizApi.update(Number(id), {
        title,
        description: description || null,
        questions: currentQuiz.data.questions.map((question) => ({
          text: question.text,
          timeLimitSeconds: question.timeLimitSeconds,
          points: question.points,
          orderIndex: question.orderIndex,
          answerOptions: question.answerOptions.map((answer) => ({
            text: answer.text,
            isCorrect: answer.isCorrect,
          })),
        })),
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
      <button onClick={() => navigate("/quizzes")}>Back</button>

      <h1>Edit Quiz</h1>

      <div className="quiz-editor-form">
        <label>Title</label>
        <input
          type="text"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
        />

        <label>Description</label>
        <textarea
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Quiz description"
        />

        <button onClick={handleSave} disabled={isSaving}>
          {isSaving ? "Saving..." : "Save"}
        </button>
      </div>
    </div>
  );
}