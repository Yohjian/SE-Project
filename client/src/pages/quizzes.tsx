import { useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { quizApi, setApi } from "../api/client";
import type { QuizSummary, SetSummary } from "../api/types";
import { getErrorMessages } from "../api/auth";
import { generateQuestions } from "../utils/quizGeneration";
import DeleteQuizModal from "../components/delete_quiz_modal";
import Modal from "../components/modal";
import { useNotification } from "../components/notification";

import "../styles/quizzes.css";

type CreateMode = "choose" | "manual" | "set";

export default function Quizzes() {
    const [quizzes, setQuizzes] = useState<QuizSummary[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [title, setTitle] = useState("");
    const [createMode, setCreateMode] = useState<CreateMode | null>(null);

    const [sets, setSets] = useState<SetSummary[]>([]);
    const [selectedSetId, setSelectedSetId] = useState<number | null>(null);
    const [isGenerating, setIsGenerating] = useState(false);
    const [quizToDelete, setQuizToDelete] = useState<QuizSummary | null>(null);
    const navigate = useNavigate();

    const { showNotification } = useNotification();

    useEffect(() => {
        quizApi
            .getMine()
            .then((res) => setQuizzes(res.data))
            .catch(() => showNotification("Failed to load quizzes."))
            .finally(() => setIsLoading(false));
    }, [showNotification]);

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

    const handleCancel = () => {
        setTitle("");
        setCreateMode(null);
        setSelectedSetId(null);
    };

    const handleBack = () => {
        setTitle("");
        setSelectedSetId(null);
        setCreateMode("choose");
    };

    const handleCreate = async (event: FormEvent) => {
        event.preventDefault();

        if (!title.trim()) {
            showNotification("A title is needed!");
            return;
        }

        try {
            const res = await quizApi.create({
                title,
                description: null,
                questions: [],
            });
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

    const handleSetCreate = async (event: FormEvent) => {
        event.preventDefault();

        if (selectedSetId === null) {
            showNotification("Please select a set.");
            return;
        }

        const selectedSet = sets.find((set) => set.id === selectedSetId);

        if (!selectedSet) {
            showNotification("Set not found.");
            return;
        }

        setIsGenerating(true);

        try {
            const response = await setApi.getCards(selectedSetId);

            const validCards = response.data.filter(
                (card) => card.term.trim() && card.definition.trim(),
            );

            const distinctAnswers = new Set(
                validCards.map((card) => card.definition.trim()),
            );

            if (distinctAnswers.size < 4) {
                showNotification(
                    "The set needs at least 4 cards with different definitions.",
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
            const messages = getErrorMessages(
                error,
                "Failed to generate quiz.",
            );
            showNotification(messages.join(" "));
        } finally {
            setIsGenerating(false);
        }
    };

    const handleDelete = async () => {
        if (quizToDelete === null) return;

        const quizId = quizToDelete.id;

        try {
            await quizApi.delete(quizId);

            setQuizzes((prev) => prev.filter((quiz) => quiz.id !== quizId));

            setQuizToDelete(null);
            showNotification("Quiz deleted.", "success");
        } catch {
            showNotification("Failed to delete quiz.");
        }
    };

    const createModalTitle =
        createMode === "manual"
            ? "Create quiz manually"
            : createMode === "set"
              ? "Create quiz from a set"
              : "Create quiz";

    return (
        <div className="container quizzes-page">
            <div className="quizzes-header">
                <h1 className="page-title">My quizzes</h1>
                <button
                    type="button"
                    className="btn btn-primary"
                    onClick={handleOpenCreate}
                >
                    + Create quiz
                </button>
            </div>

            {!isLoading && quizzes.length === 0 && (
                <div className="quizzes-empty">
                    <h2>No quizzes yet</h2>
                    <p className="text-muted">
                        Create a quiz to host it in a room or let others
                        practice it.
                    </p>
                    <button
                        type="button"
                        className="btn btn-primary"
                        onClick={handleOpenCreate}
                    >
                        Create your first quiz
                    </button>
                </div>
            )}

            {quizzes.length > 0 && (
                <ul className="quiz-grid">
                    {quizzes.map((quiz) => (
                        <li key={quiz.id} className="quiz-card">
                            <h2 className="quiz-card-title">
                                <Link
                                    to={`/quizzes/${quiz.id}/edit`}
                                    className="quiz-card-link"
                                >
                                    {quiz.title}
                                </Link>
                            </h2>

                            {quiz.description && (
                                <p className="quiz-card-description">
                                    {quiz.description}
                                </p>
                            )}

                            <button
                                className="quiz-delete"
                                aria-label={`Delete quiz ${quiz.title}`}
                                title="Delete quiz"
                                type="button"
                                onClick={() => setQuizToDelete(quiz)}
                            >
                                <svg
                                    width="16"
                                    height="16"
                                    viewBox="0 0 24 24"
                                    fill="none"
                                    stroke="currentColor"
                                    strokeWidth="2"
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    aria-hidden="true"
                                >
                                    <path d="M3 6h18" />
                                    <path d="M8 6V4h8v2" />
                                    <path d="M19 6l-1 14H6L5 6" />
                                    <path d="M10 11v6M14 11v6" />
                                </svg>
                            </button>
                        </li>
                    ))}
                </ul>
            )}

            {createMode !== null && (
                <Modal
                    title={createModalTitle}
                    onClose={handleCancel}
                    closeDisabled={isGenerating}
                >
                    {createMode === "choose" && (
                        <>
                            <div className="create-options">
                                <button
                                    type="button"
                                    className="create-option"
                                    onClick={() => setCreateMode("manual")}
                                >
                                    <span className="create-option-title">
                                        Start from scratch
                                    </span>
                                    <span className="create-option-text">
                                        Write your own questions and answers.
                                    </span>
                                </button>

                                <button
                                    type="button"
                                    className="create-option"
                                    onClick={() => setCreateMode("set")}
                                >
                                    <span className="create-option-title">
                                        Use a set
                                    </span>
                                    <span className="create-option-text">
                                        Generate questions from a set of terms
                                        and definitions.
                                    </span>
                                </button>
                            </div>

                            <div className="modal-actions">
                                <button
                                    type="button"
                                    className="btn btn-secondary"
                                    onClick={handleCancel}
                                >
                                    Cancel
                                </button>
                            </div>
                        </>
                    )}

                    {createMode === "manual" && (
                        <form onSubmit={handleCreate}>
                            <div className="field">
                                <label htmlFor="quiz-title">Quiz title</label>
                                <input
                                    id="quiz-title"
                                    className="input"
                                    type="text"
                                    placeholder="e.g. Spanish basics"
                                    value={title}
                                    onChange={(e) => setTitle(e.target.value)}
                                    autoFocus
                                />
                            </div>

                            <div className="modal-actions">
                                <button
                                    type="button"
                                    className="btn btn-secondary"
                                    onClick={handleBack}
                                >
                                    Back
                                </button>
                                <button
                                    type="submit"
                                    className="btn btn-primary"
                                >
                                    Create quiz
                                </button>
                            </div>
                        </form>
                    )}

                    {createMode === "set" && (
                        <form onSubmit={handleSetCreate}>
                            <div className="field">
                                <label htmlFor="quiz-set">Set</label>
                                <select
                                    id="quiz-set"
                                    className="input"
                                    value={selectedSetId ?? ""}
                                    onChange={(e) =>
                                        setSelectedSetId(
                                            e.target.value === ""
                                                ? null
                                                : Number(e.target.value),
                                        )
                                    }
                                    disabled={isGenerating}
                                >
                                    <option value="">Select a set</option>
                                    {sets.map((set) => (
                                        <option key={set.id} value={set.id}>
                                            {set.name} ({set.cardCount} cards)
                                        </option>
                                    ))}
                                </select>
                                {sets.length === 0 ? (
                                    <span className="field-hint">
                                        You don’t have any sets yet.{" "}
                                        <Link to="/sets">Create one</Link>{" "}
                                        first.
                                    </span>
                                ) : (
                                    <span className="field-hint">
                                        The set needs at least 4 cards with
                                        different definitions.
                                    </span>
                                )}
                            </div>

                            <div className="field">
                                <label htmlFor="quiz-set-title">
                                    Quiz title (optional)
                                </label>
                                <input
                                    id="quiz-set-title"
                                    className="input"
                                    type="text"
                                    placeholder="Defaults to the set name"
                                    value={title}
                                    onChange={(e) => setTitle(e.target.value)}
                                    disabled={isGenerating}
                                />
                            </div>

                            <div className="modal-actions">
                                <button
                                    type="button"
                                    className="btn btn-secondary"
                                    onClick={handleBack}
                                    disabled={isGenerating}
                                >
                                    Back
                                </button>
                                <button
                                    type="submit"
                                    className="btn btn-primary"
                                    disabled={
                                        isGenerating || selectedSetId === null
                                    }
                                >
                                    {isGenerating
                                        ? "Generating…"
                                        : "Generate quiz"}
                                </button>
                            </div>
                        </form>
                    )}
                </Modal>
            )}

            {quizToDelete !== null && (
                <DeleteQuizModal
                    quizTitle={quizToDelete.title}
                    onCancel={() => setQuizToDelete(null)}
                    onConfirm={handleDelete}
                />
            )}
        </div>
    );
}
