import { useEffect, useState } from "react";
import { quizApi, setApi  } from "../api/client";
import type {QuizSummary, SetSummary, CardResponse, QuestionRequest,} from "../api/types";
import { getErrorMessages } from "../api/auth";
import { useNotification } from "../components/notification";
import { useNavigate } from "react-router-dom";

import "../styles/quizzes.css";

export default function Quizzes() {
  const [quizzes, setQuizzes] = useState<QuizSummary[]>([]);
  const [title, setTitle] = useState("");
  const [createMode, setCreateMode] = useState<
    "choose" | "manual" | "set" | null
  >(null);

  const [sets, setSets] = useState<SetSummary[]>([]);
  const [selectedSetId, setSelectedSetId] = useState<number | null>(null);
  const [isGenerating, setIsGenerating] = useState(false);
  const [quizToDelete, setQuizToDelete] = useState<number | null>(null);
  const navigate = useNavigate();

  const { showNotification } = useNotification();

  useEffect(() => {
    quizApi
      .getMine()
      .then((res) => setQuizzes(res.data))
      .catch(() => showNotification("Failed to load quizzes."));
  }, []);

  const handleOpenCreate = async () => {
    setCreateMode("choose");
    setSelectedSetId(null);
    setTitle("");

    try {
      const response = await setApi.getAll();
      setSets(response.data);
    } catch {
      showNotification("Failed to load sets.");
    }
  };

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
      setCreateMode(null);
      showNotification("Quiz created.", "success");
      navigate(`/quizzes/${res.data.id}/edit`);
    } catch (error) {
      const messages = getErrorMessages(error, "Failed to create quiz.");
      showNotification(messages.join(" "));
    }
  };

  const handleCancel = () => {setTitle(""); setCreateMode(null); setSelectedSetId(null)};

  const generateQuestions = (
    cards: CardResponse[]
  ): QuestionRequest[] => {
    const validCards = cards.filter(
      (card) => card.term.trim() && card.definition.trim()
    );

    return validCards.map((card, index) => {
      const correctAnswer = card.definition.trim();

      const incorrectAnswers = [
        ...new Set(
          validCards
            .filter((other) => other.id !== card.id)
            .map((other) => other.definition.trim())
            .filter((answer) => answer !== correctAnswer)
        ),
      ];

      const shuffledIncorrect = [...incorrectAnswers].sort(
        () => Math.random() - 0.5
      );

      const options = [
        { text: correctAnswer, isCorrect: true },
        ...shuffledIncorrect.slice(0, 3).map((text) => ({
          text,
          isCorrect: false,
        })),
      ];

      return {
        text: `What is the translation of "${card.term}"?`,
        timeLimitSeconds: 30,
        points: 100,
        orderIndex: index,
        answerOptions: options.sort(() => Math.random() - 0.5),
      };
    });
  };

  const handleSetCreate = async () => {
  if (selectedSetId === null) {
    showNotification("Please select a set.");
    return;
  }

  const selectedSet = sets.find(
    (set) => set.id === selectedSetId
  );

  if (!selectedSet) {
    showNotification("Set not found.");
    return;
  }

  setIsGenerating(true);

  try {
    const response = await setApi.getCards(selectedSetId);

    const validCards = response.data.filter(
      (card) => card.term.trim() && card.definition.trim()
    );

    const distinctAnswers = new Set(
      validCards.map((card) => card.definition.trim())
    );

    if (distinctAnswers.size < 4) {
      showNotification(
        "The set needs at least 4 cards with different definitions."
      );
      return;
    }

    const questions = generateQuestions(validCards);

    const result = await quizApi.create({
        title: title.trim() || `${selectedSet.name} Quiz`,
        description: `Generated from ${selectedSet.name}`,
        questions,
      });

      setQuizzes((prev) => [
        ...prev,
        {
          id: result.data.id,
          title: result.data.title,
          description: result.data.description,
        },
      ]);

      showNotification("Quiz generated successfully!", "success");
      navigate(`/quizzes/${result.data.id}/edit`);
    } catch (error) {
      const messages = getErrorMessages(error, "Failed to generate quiz.");
      showNotification(messages.join(" "));
    } finally {
      setIsGenerating(false);
    }
  };

  const handleDelete = async () => {
    if (quizToDelete === null) return;

    const quizId = quizToDelete;

    try {
      await quizApi.delete(quizId);

      setQuizzes((prev) =>
        prev.filter((quiz) => quiz.id !== quizId)
      );

      setQuizToDelete(null);
      showNotification("Quiz deleted.", "success");
    } catch {
      showNotification("Failed to delete quiz.");
    }
  };
  return (
    <div className="quizzes-page">
      <div className="quizzes-header">
        <h1>Quizzes</h1>
        <button onClick={handleOpenCreate}>+ Create quiz</button>
      </div>

  {createMode !== null && (
    <div className="quiz-create">

      {createMode === "choose" && (
        <>
          <h3>Create Quiz</h3>
          <p>How would you like to create your quiz?</p>

          <button onClick={() => setCreateMode("manual")}>
            Create manually
          </button>

          <button
          className="create-from-set-button"
          onClick={() => setCreateMode("set")}>
            Use a set
          </button>
        </>
      )}

      {createMode === "manual" && (
        <>
          <h3>Create manually</h3>

          <input
            type="text"
            placeholder="Quiz title"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
          />

          <button onClick={handleCreate}>
            Create
          </button>
        </>
      )}

      {createMode === "set" && (
        <>
          <h3>Create from set</h3>

          <select
            value={selectedSetId ?? ""}
            onChange={(e) =>
              setSelectedSetId(
                e.target.value === ""
                  ? null
                  : Number(e.target.value)
              )
            }
          >
            <option value="">Select a set</option>

            {sets.map((set) => (
              <option key={set.id} value={set.id}>
                {set.name} ({set.cardCount} cards)
              </option>
            ))}
          </select>

          <input
            type="text"
            placeholder="Quiz title (optional)"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
          />

          <button
            onClick={handleSetCreate}
            disabled={isGenerating || selectedSetId === null}
          >
            Generate quiz
          </button>
        </>
      )}

      <button onClick={handleCancel} disabled={isGenerating}>
        Cancel
      </button>

    </div>
  )}

      <div className="quiz-grid">
        {quizzes.map((quiz) => (
          <div
            key={quiz.id}
            className="quiz-card"
            onClick={() => navigate(`/quizzes/${quiz.id}/edit`)}
          >
            <h3>{quiz.title}</h3>

            {quiz.description && <p>{quiz.description}</p>}

              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  setQuizToDelete(quiz.id);
                }}
              >
                Delete
              </button>
          </div>
        ))}
      </div>

      {quizToDelete !== null && (
        <div className="delete-modal-overlay">
          <div
            className="delete-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="delete-modal-title"
          >
            <h2 id="delete-modal-title">Delete quiz?</h2>

            <p>
              Are you sure you want to delete this quiz?
              This action cannot be undone.
            </p>

            <div className="delete-modal-actions">
              <button
                type="button"
                onClick={() => setQuizToDelete(null)}
              >
                Cancel
              </button>

              <button
                type="button"
                className="confirm-delete-button"
                onClick={handleDelete}
              >
                Delete quiz
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}