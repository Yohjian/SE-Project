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
              <div className="overlay" onClick={handleCancel} />,
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
                <div className="content">
                    <div className="toolbar">
                        <input
                            type="text"
                            placeholder="Search cards..."
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                        />
                        <button
                            onClick={() =>
                                setViewMode((m) =>
                                    m === "grid" ? "list" : "grid",
                                )
                            }
                        >
                            {viewMode === "grid" ? "List view" : "Grid view"}
                        </button>
                        <div className="export-menu-wrapper" ref={exportRef}>
                            <button
                                className="export-btn"
                                onClick={() =>
                                    setShowExportMenu((prev) => !prev)
                                }
                                disabled={!selectedSetId}
                            >
                                Download ▾
                            </button>
                            {showExportMenu && (
                                <div className="export-dropdown">
                                    <button
                                        onClick={() => handleExport("json")}
                                    >
                                        JSON
                                    </button>
                                    <button onClick={() => handleExport("csv")}>
                                        CSV
                                    </button>
                                </div>
                            )}
                        </div>
                    </div>

                    <div
                        className={
                            viewMode === "grid" ? "cards-grid" : "cards-list"
                        }
                    >
                        {filteredCards.map((card) => (
                            <div
                                key={card.id}
                                className={`card ${viewMode === "list" ? "card-list" : ""}`}
                            >
                                <button
                                    className="card-delete"
                                    onClick={() => handleDeleteCard(card.id)}
                                    title="Delete card"
                                >
                                    ×
                                </button>
                                <strong>{card.term}</strong>
                                <span>{card.definition}</span>
                            </div>
                        ))}
                        {isAdding ? (
                            <div className="card card-add-form">
                                <input
                                    ref={termRef}
                                    type="text"
                                    placeholder="Term"
                                    value={newTerm}
                                    onChange={(e) => setNewTerm(e.target.value)}
                                    onKeyDown={handleKeyDown}
                                />
                                <input
                                    type="text"
                                    placeholder="Definition"
                                    value={newDefinition}
                                    onChange={(e) =>
                                        setNewDefinition(e.target.value)
                                    }
                                    onKeyDown={handleKeyDown}
                                />
                                <div className="card-add-form-actions">
                                    <button
                                        onClick={handleCancel}
                                        disabled={isSaving}
                                    >
                                        Cancel
                                    </button>
                                    <button
                                        onClick={handleSave}
                                        disabled={isSaving}
                                    >
                                        Save
                                    </button>
                                </div>
                            </div>
                        ) : (
                            <div
                                className="card card-add"
                                onClick={() => setIsAdding(true)}
                            >
                                <span>+ Add card</span>
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </>
    );
}
