import { useState, useEffect, useRef, useMemo, useCallback } from "react";
import { createPortal } from "react-dom";
import Tree from "../components/tree";
import { cardApi, setApi } from "../api/client";
import axios from "axios";

import "../styles/sets.css";
import { useNotification } from "../components/notification";
import { CardResponse, SetSummary } from "../api/types";

function filenameFromHeader(header: string | undefined, fallback: string) {
    if (!header) return fallback;

    // ASP.NET sends: attachment; filename=Animals.json; filename*=UTF-8''Animals.json
    const utf8 = /filename\*=UTF-8''([^;]+)/i.exec(header);
    if (utf8) return decodeURIComponent(utf8[1]);

    const plain = /filename="?([^";]+)"?/i.exec(header);
    return plain ? plain[1] : fallback;
}

function downloadBlob(blob: Blob, filename: string) {
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    a.click();
    setTimeout(() => URL.revokeObjectURL(url), 0); // let the browser start the download first
}

export default function Sets() {
    const [selectedSetId, setSelectedSetId] = useState<number | null>(null);
    const [viewMode, setViewMode] = useState<"grid" | "list">("grid");
    const [search, setSearch] = useState("");
    const [isAdding, setIsAdding] = useState(false);
    const [newTerm, setNewTerm] = useState("");
    const [newDefinition, setNewDefinition] = useState("");
    const [isSaving, setIsSaving] = useState(false);
    const [sets, setSets] = useState<SetSummary[]>([]);
    const [cards, setCards] = useState<CardResponse[]>([]);
    const [showExportMenu, setShowExportMenu] = useState(false);

    const termRef = useRef<HTMLInputElement>(null);
    const exportRef = useRef<HTMLDivElement>(null);

    const { showNotification } = useNotification();

    const filteredCards = useMemo(
        () =>
            cards.filter(
                (c) =>
                    c.term.toLowerCase().includes(search.toLowerCase()) ||
                    c.definition.toLowerCase().includes(search.toLowerCase()),
            ),
        [cards, search],
    );

    useEffect(() => {
        if (isAdding) termRef.current?.focus();
    }, [isAdding]);

    useEffect(() => {
        const handler = (e: MouseEvent) => {
            if (
                exportRef.current &&
                !exportRef.current.contains(e.target as Node)
            )
                setShowExportMenu(false);
        };
        document.addEventListener("mousedown", handler);
        return () => document.removeEventListener("mousedown", handler);
    }, []);

    useEffect(() => {
        setApi
            .getAll()
            .then((res) => setSets(res.data))
            .catch(() => showNotification("Failed to load sets."));
    }, []);

    useEffect(() => {
        if (sets.length > 0 && selectedSetId === null)
            setSelectedSetId(sets[0].id);
    }, [sets]);

    useEffect(() => {
        if (selectedSetId === null) return;
        setApi
            .getCards(selectedSetId)
            .then((res) => setCards(res.data))
            .catch(() => showNotification("Failed to load cards."));
    }, [selectedSetId]);

    const handleCancel = useCallback(() => {
        setIsAdding(false);
        setNewTerm("");
        setNewDefinition("");
    }, []);

    const handleSave = useCallback(async () => {
        if (!newTerm.trim()) return showNotification("A term is needed!");
        if (!newDefinition.trim())
            return showNotification("A definition is needed!");
        if (selectedSetId === null) return showNotification("No set selected.");

        setIsSaving(true);
        try {
            const res = await cardApi.add(
                selectedSetId,
                newTerm,
                newDefinition,
            );
            setCards((prev) => [...prev, res.data]);
            showNotification("Card added.", "success");
            handleCancel();
        } catch (err: unknown) {
            const message = axios.isAxiosError(err)
                ? typeof err.response?.data === "string"
                    ? err.response.data
                    : (err.response?.data?.detail ??
                      err.response?.data?.title ??
                      "Failed to add card.")
                : "Failed to add card.";
            showNotification(message);
        } finally {
            setIsSaving(false);
        }
    }, [newTerm, newDefinition, selectedSetId, handleCancel, showNotification]);

    const handleKeyDown = useCallback(
        (e: React.KeyboardEvent<HTMLInputElement>) => {
            if (e.key === "Enter") handleSave();
            if (e.key === "Escape") handleCancel();
        },
        [handleSave, handleCancel],
    );

    const handleAddSet = useCallback(
        async (name: string) => {
            try {
                const res = await setApi.create(name);
                setSets((prev) => [...prev, res.data]);
                setSelectedSetId(res.data.id);
            } catch {
                showNotification("Failed to create set.");
            }
        },
        [showNotification],
    );

    const handleDeleteSet = useCallback(
        async (setId: number) => {
            if (!confirm("Delete this set and all its cards?")) return;
            try {
                await setApi.delete(setId);
                setSets((prev) => prev.filter((s) => s.id !== setId));
                if (selectedSetId === setId) {
                    setCards([]);
                    setSelectedSetId(null);
                }
                showNotification("Set deleted.", "success");
            } catch {
                showNotification("Failed to delete set.");
            }
        },
        [selectedSetId, showNotification],
    );

    const handleDeleteCard = useCallback(
        async (cardId: number) => {
            if (selectedSetId === null) return;
            try {
                await cardApi.delete(selectedSetId, cardId);
                setCards((prev) => prev.filter((c) => c.id !== cardId));
                showNotification("Card deleted.", "success");
            } catch {
                showNotification("Failed to delete card.");
            }
        },
        [selectedSetId, showNotification],
    );

    const handleExport = useCallback(
        async (format: "json" | "csv") => {
            if (!selectedSetId) return;
            try {
                const res = await setApi.download(selectedSetId, format);
                const filename = filenameFromHeader(
                    res.headers["content-disposition"],
                    `set.${format}`,
                );
                downloadBlob(res.data, filename);
            } catch {
                showNotification("Failed to download set.");
            } finally {
                setShowExportMenu(false);
            }
        },
        [selectedSetId, showNotification],
    );

    const overlay = isAdding
        ? createPortal(
              <div className="sets-overlay" onClick={handleCancel} />,
              document.body,
          )
        : null;

    return (
        <>
            {overlay}
            <div className="sets-page">
                <Tree
                    title="Sets"
                    entries={sets.map((s) => ({ id: s.id, name: s.name }))}
                    selectedId={selectedSetId}
                    onSelect={setSelectedSetId}
                    onAdd={handleAddSet}
                    onDelete={handleDeleteSet}
                />
                <div className="sets-content">
                    <div className="sets-toolbar">
                        <input
                            type="text"
                            className="input sets-toolbar__search"
                            placeholder="Search cards..."
                            aria-label="Search cards"
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                        />
                        <button
                            type="button"
                            className="btn btn-secondary btn-sm"
                            onClick={() =>
                                setViewMode((m) =>
                                    m === "grid" ? "list" : "grid",
                                )
                            }
                        >
                            {viewMode === "grid" ? "List view" : "Grid view"}
                        </button>
                        <div className="sets-export" ref={exportRef}>
                            <button
                                type="button"
                                className="btn btn-secondary btn-sm"
                                onClick={() =>
                                    setShowExportMenu((prev) => !prev)
                                }
                                disabled={!selectedSetId}
                                aria-haspopup="menu"
                                aria-expanded={showExportMenu}
                            >
                                Download ▾
                            </button>
                            {showExportMenu && (
                                <div className="sets-export__menu">
                                    <button
                                        type="button"
                                        className="sets-export__item"
                                        onClick={() => handleExport("json")}
                                    >
                                        JSON
                                    </button>
                                    <button
                                        type="button"
                                        className="sets-export__item"
                                        onClick={() => handleExport("csv")}
                                    >
                                        CSV
                                    </button>
                                </div>
                            )}
                        </div>
                    </div>

                    {selectedSetId === null ? (
                        <p className="sets-empty text-muted">
                            Select or create a set in the sidebar.
                        </p>
                    ) : (
                        <div
                            className={`set-cards ${
                                viewMode === "grid"
                                    ? "set-cards--grid"
                                    : "set-cards--list"
                            }`}
                        >
                            {filteredCards.map((card) => (
                                <div
                                    key={card.id}
                                    className={`set-card ${viewMode === "list" ? "set-card--list" : ""}`}
                                >
                                    <button
                                        type="button"
                                        className="set-card__delete"
                                        onClick={() =>
                                            handleDeleteCard(card.id)
                                        }
                                        title="Delete card"
                                        aria-label={`Delete ${card.term}`}
                                    >
                                        ×
                                    </button>
                                    <strong className="set-card__term">
                                        {card.term}
                                    </strong>
                                    <span className="set-card__definition">
                                        {card.definition}
                                    </span>
                                </div>
                            ))}

                            {isAdding ? (
                                <div className="set-card set-card-form">
                                    <input
                                        ref={termRef}
                                        className="input"
                                        type="text"
                                        placeholder="Term"
                                        value={newTerm}
                                        onChange={(e) =>
                                            setNewTerm(e.target.value)
                                        }
                                        onKeyDown={handleKeyDown}
                                    />
                                    <input
                                        className="input"
                                        type="text"
                                        placeholder="Definition"
                                        value={newDefinition}
                                        onChange={(e) =>
                                            setNewDefinition(e.target.value)
                                        }
                                        onKeyDown={handleKeyDown}
                                    />
                                    <div className="set-card-form__actions">
                                        <button
                                            type="button"
                                            className="btn btn-secondary btn-sm"
                                            onClick={handleCancel}
                                            disabled={isSaving}
                                        >
                                            Cancel
                                        </button>
                                        <button
                                            type="button"
                                            className="btn btn-primary btn-sm"
                                            onClick={handleSave}
                                            disabled={isSaving}
                                        >
                                            Save
                                        </button>
                                    </div>
                                </div>
                            ) : (
                                <button
                                    type="button"
                                    className="set-card set-card--add"
                                    onClick={() => setIsAdding(true)}
                                >
                                    + Add card
                                </button>
                            )}
                        </div>
                    )}
                </div>
            </div>
        </>
    );
}
