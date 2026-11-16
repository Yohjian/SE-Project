import { useEffect, useState } from "react";
import { practiceApi } from "../api/client";
import type { PracticeQuizSummary, PracticeFilterOption, } from "../api/types";
import { useNavigate } from "react-router-dom";
import "../styles/quizzes.css";

export default function Practice() {
    const navigate = useNavigate();
    const [quizzes, setQuizzes] = useState<PracticeQuizSummary[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const [selectedLanguage, setSelectedLanguage] = useState("");
    const [selectedCategory, setSelectedCategory] = useState("");

    const [languages, setLanguages] = useState<PracticeFilterOption[]>([]);
    const [categories, setCategories] = useState<PracticeFilterOption[]>([]);

    useEffect(() => {
        practiceApi.getFilters()
            .then((response) => {
                setLanguages(response.data.languages);
                setCategories(response.data.categories);
            })
            .catch((error) => {
                console.error("Failed to load practice filters:", error);
            });
    }, []);

    useEffect(() => {
        setLoading(true);
        setError(null);

        practiceApi.getQuizzes({
            language: selectedLanguage ? Number(selectedLanguage) : undefined,
            category: selectedCategory ? Number(selectedCategory) : undefined,

        }).then((response) => {
            setQuizzes(response.data);
        })
            .catch((error) => {
                console.error("Error fetching quizzes:", error);
                setError("Failed to load quizzes.");
            })
            .finally(() => {
                setLoading(false);
            });
    }, [selectedLanguage, selectedCategory]);

    return (
        <div className="container quizzes-page practice-page">
            <div className="quizzes-header">
                <h1 className="page-title">Practice Mode</h1>
            </div>

            <p className="text-muted">
                Choose a quiz to start practicing.
            </p>

            <div className="practice-filters">
                <label>
                    Language
                    <select
                        value={selectedLanguage}
                        onChange={(e) => setSelectedLanguage(e.target.value)}
                    >
                        <option value="">All languages</option>

                        {languages.map((language) => (
                            <option key={language.id} value={language.id}>
                                {language.name}
                            </option>
                        ))}
                    </select>
                </label>

                <label>
                    Category
                    <select
                        value={selectedCategory}
                        onChange={(e) => setSelectedCategory(e.target.value)}
                    >
                        <option value="">All categories</option>

                        {categories.map((category) => (
                            <option key={category.id} value={category.id}>
                                {category.name}
                            </option>
                        ))}
                    </select>
                </label>
            </div>

            {loading && (<p className="text-muted">Loading quizzes...</p>)}

            {!loading && error && (<p>{error}</p>)}

            {!loading && !error && quizzes.length === 0 && (
                <div className="quizzes-empty">
                    <h2>No practice quizzes available</h2>
                    <p className="text-muted">
                        Create a practice quiz to get started.
                    </p>
                    <button
                        type="button"
                        className="btn btn-primary"
                        onClick={() => navigate("/quizzes")}
                    >
                        Go to quizzes
                    </button>
                </div>
            )}

            {!loading && !error && quizzes.length > 0 && (
                <ul className="quiz-grid">
                    {quizzes.map((quiz) => (
                        <li key={quiz.id} className="quiz-card">
                            <h2 className="quiz-card-title">
                                {quiz.title}
                            </h2>

                            {quiz.description && (
                                <p className="quiz-card-description">
                                    {quiz.description}
                                </p>
                            )}

                            <p className="text-muted">
                                Questions: {quiz.questionCount}
                            </p>

                            <button
                                type="button"
                                className="btn btn-primary"
                                onClick={() =>
                                    navigate(`/practice/${quiz.id}`)
                                }
                                disabled={quiz.questionCount === 0}
                            >
                                Start practice
                            </button>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}
