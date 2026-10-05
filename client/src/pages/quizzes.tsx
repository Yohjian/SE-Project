import { useEffect, useState } from "react";
import { quizApi } from "../api/client";
import { QuizSummary } from "../api/types";
import { useNotification } from "../components/notification";
import { useNavigate } from "react-router-dom";

import "../styles/quizzes.css";

export default function Quizzes() {
  const [quizzes, setQuizzes] = useState<QuizSummary[]>([]);
  const [title, setTitle] = useState("");
  const [isAdding, setIsAdding] = useState(false);
  const navigate = useNavigate();

  const { showNotification } = useNotification();

  useEffect(() => {
    quizApi
      .getMine()
      .then((res) => setQuizzes(res.data))
      .catch(() => showNotification("Failed to load quizzes."));
  }, []);

  const handleCreate = async () => {
    if (!title.trim()) {
      showNotification("A title is needed!");
      return;
    }

    try {
      const res = await quizApi.create({title, description: null, questions: []});
      setQuizzes((prev) => [
        ...prev,
        {
          id: res.data.id,
          title: res.data.title,
          description: res.data.description,
        },
      ]);

      setTitle("");
      setIsAdding(false);
      showNotification("Quiz created.", "success");
    } catch {
      showNotification("Failed to create quiz.");
    }
  };

  const handleCancel = () => {setTitle(""); setIsAdding(false)};

  return (
    <div className="quizzes-page">
      <div className="quizzes-header">
        <h1>Quizzes</h1>
        <button onClick={() => setIsAdding(true)}>+ Create quiz</button>
      </div>

      {isAdding && (
        <div className="quiz-create">
          <input
            type="text"
            placeholder="Quiz title"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
          />
          <button onClick={handleCancel}>Cancel</button>
          <button onClick={handleCreate}>Create</button>
        </div>
      )}

      <div className="quiz-grid">
        {quizzes.map((quiz) => (
          <div className="quiz-card" key={quiz.id} onClick={() => navigate(`/quizzes/${quiz.id}`)}>
            <strong>{quiz.title}</strong>
            {quiz.description && <span>{quiz.description}</span>}
          </div>
        ))}
      </div>
    </div>
  );
}