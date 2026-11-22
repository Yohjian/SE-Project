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
    setTimeout(() => URL.revokeObjectURL(url), 0);
}

function parseMetaValue(raw: string): unknown {
    if (raw === "true") return true;
    if (raw === "false") return false;
    const n = Number(raw);
    if (raw.trim() !== "" && !isNaN(n)) return n;
    return raw;
}

interface MetaField {
    key: string;
    value: string;
}

export default function Sets() {
    const [selectedSetId, setSelectedSetId] = useState<number | null>(null);
    const [viewMode, setViewMode] = useState<"grid" | "list">("grid");
    const [search, setSearch] = useState("");
    const [isAdding, setIsAdding] = useState(false);
    const [newTerm, setNewTerm] = useState("");
    const [newDefinition, setNewDefinition] = useState("");
    const [newMetadata, setNewMetadata] = useState<MetaField[]>([]);
    const [isSaving, setIsSaving] = useState(false);
    const [sets, setSets] = useState<SetSummary[]>([]);
    const [cards, setCards] = useState<CardResponse[]>([]);
    const [showExportMenu, setShowExportMenu] = useState(false);
    const [showSortMenu, setShowSortMenu] = useState(false);
    const [sortBy, setSortBy] = useState("");
    const [sortAsc, setSortAsc] = useState(true);
    const [editingMetaCardId, setEditingMetaCardId] = useState<number | null>(
        null,
    );
    const [metaKey, setMetaKey] = useState("");
    const [metaValue, setMetaValue] = useState("");

    const termRef = useRef<HTMLInputElement>(null);
    const exportRef = useRef<HTMLDivElement>(null);
    const sortRef = useRef<HTMLDivElement>(null);

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

    const sortedCards = useMemo(() => {
        if (!sortBy) return filteredCards;
        const withKey = filteredCards.filter(
            (c) => c.metadata != null && sortBy in c.metadata,
        );
        return [...withKey].sort((a, b) => {
            const av = a.metadata[sortBy];
            const bv = b.metadata[sortBy];
            if (typeof av === "number" && typeof bv === "number")
                return sortAsc ? av - bv : bv - av;
            return sortAsc
                ? String(av).localeCompare(String(bv))
                : String(bv).localeCompare(String(av));
        });
    }, [filteredCards, sortBy, sortAsc]);

    const metadataKeys = useMemo(
        () => [...new Set(cards.flatMap((c) => Object.keys(c.metadata ?? {})))],
        [cards],
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
            if (sortRef.current && !sortRef.current.contains(e.target as Node))
                setShowSortMenu(false);
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
        setNewMetadata([]);
    }, []);

    const handleSave = useCallback(async () => {
        if (!newTerm.trim()) return showNotification("A term is needed!");
        if (!newDefinition.trim())
            return showNotification("A definition is needed!");
        if (selectedSetId === null) return showNotification("No set selected.");

        const metadata = Object.fromEntries(
            newMetadata
                .filter((f) => f.key.trim())
                .map((f) => [f.key.trim(), parseMetaValue(f.value)]),
        );

        setIsSaving(true);
        try {
            const res = await cardApi.add(
                selectedSetId,
                newTerm,
                newDefinition,
                Object.keys(metadata).length > 0 ? metadata : undefined,
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
    }, [
        newTerm,
        newDefinition,
        newMetadata,
        selectedSetId,
        handleCancel,
        showNotification,
    ]);

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

    const handleSaveCardMeta = useCallback(
        async (cardId: number) => {
            if (!selectedSetId || !metaKey.trim()) return;
            const value = parseMetaValue(metaValue);
            try {
                await cardApi.setMetadata(
                    selectedSetId,
                    cardId,
                    metaKey.trim(),
                    value,
                );
                setCards((prev) =>
                    prev.map((c) =>
                        c.id === cardId
                            ? {
                                  ...c,
                                  metadata: {
                                      ...c.metadata,
                                      [metaKey.trim()]: value,
                                  },
                              }
                            : c,
                    ),
                );
                setMetaKey("");
                setMetaValue("");
                setEditingMetaCardId(null);
                showNotification("Metadata saved.", "success");
            } catch {
                showNotification("Failed to save metadata.");
            }
        },
        [selectedSetId, metaKey, metaValue, showNotification],
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

                        {metadataKeys.length > 0 && (
                            <>
                                <div className="sets-export" ref={sortRef}>
                                    <button
                                        type="button"
                                        className="btn btn-secondary btn-sm"
                                        onClick={() =>
                                            setShowSortMenu((prev) => !prev)
                                        }
                                        aria-haspopup="menu"
                                        aria-expanded={showSortMenu}
                                    >
                                        {sortBy ? sortBy : "Sort by…"} ▾
                                    </button>
                                    {showSortMenu && (
                                        <div className="sets-export__menu">
                                            <button
                                                type="button"
                                                className="sets-export__item"
                                                onClick={() => {
                                                    setSortBy("");
                                                    setShowSortMenu(false);
                                                }}
                                            >
                                                No sort
                                            </button>
                                            {metadataKeys.map((k) => (
                                                <button
                                                    key={k}
                                                    type="button"
                                                    className="sets-export__item"
                                                    onClick={() => {
                                                        setSortBy(k);
                                                        setShowSortMenu(false);
                                                    }}
                                                >
                                                    {k}
                                                </button>
                                            ))}
                                        </div>
                                    )}
                                </div>
                                {sortBy && (
                                    <button
                                        type="button"
                                        className="btn btn-secondary btn-sm"
                                        onClick={() => setSortAsc((a) => !a)}
                                    >
                                        {sortAsc ? "↑ Asc" : "↓ Desc"}
                                    </button>
                                )}
                            </>
                        )}

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
                            {sortedCards.map((card) => (
                                <div
                                    key={card.id}
                                    className={`set-card ${
                                        viewMode === "list"
                                            ? "set-card--list"
                                            : ""
                                    }`}
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

                                    {Object.keys(card.metadata ?? {}).length >
                                        0 && (
                                        <div className="set-card__meta">
                                            {Object.entries(card.metadata).map(
                                                ([k, v]) => (
                                                    <span
                                                        key={k}
                                                        className="set-card__meta-entry"
                                                    >
                                                        {k}: {String(v)}
                                                    </span>
                                                ),
                                            )}
                                        </div>
                                    )}

                                    {editingMetaCardId === card.id ? (
                                        <div className="set-card__meta-form">
                                            <input
                                                className="input"
                                                placeholder="Key"
                                                value={metaKey}
                                                onChange={(e) =>
                                                    setMetaKey(e.target.value)
                                                }
                                                autoFocus
                                            />
                                            <input
                                                className="input"
                                                placeholder="Value"
                                                value={metaValue}
                                                onChange={(e) =>
                                                    setMetaValue(e.target.value)
                                                }
                                                onKeyDown={(e) => {
                                                    if (e.key === "Enter")
                                                        handleSaveCardMeta(
                                                            card.id,
                                                        );
                                                    if (e.key === "Escape")
                                                        setEditingMetaCardId(
                                                            null,
                                                        );
                                                }}
                                            />
                                            <div className="set-card__meta-form-actions">
                                                <button
                                                    type="button"
                                                    className="btn btn-secondary btn-sm"
                                                    onClick={() =>
                                                        setEditingMetaCardId(
                                                            null,
                                                        )
                                                    }
                                                >
                                                    Cancel
                                                </button>
                                                <button
                                                    type="button"
                                                    className="btn btn-primary btn-sm"
                                                    onClick={() =>
                                                        handleSaveCardMeta(
                                                            card.id,
                                                        )
                                                    }
                                                >
                                                    Save
                                                </button>
                                            </div>
                                        </div>
                                    ) : (
                                        <button
                                            type="button"
                                            className="set-card__meta-add"
                                            onClick={() => {
                                                setEditingMetaCardId(card.id);
                                                setMetaKey("");
                                                setMetaValue("");
                                            }}
                                        >
                                            + metadata
                                        </button>
                                    )}
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

                                    {newMetadata.map((field, i) => (
                                        <div
                                            key={i}
                                            className="set-card-form__meta-row"
                                        >
                                            <input
                                                className="input"
                                                placeholder="Key"
                                                value={field.key}
                                                onChange={(e) =>
                                                    setNewMetadata((prev) =>
                                                        prev.map((f, j) =>
                                                            j === i
                                                                ? {
                                                                      ...f,
                                                                      key: e
                                                                          .target
                                                                          .value,
                                                                  }
                                                                : f,
                                                        ),
                                                    )
                                                }
                                            />
                                            <input
                                                className="input"
                                                placeholder="Value"
                                                value={field.value}
                                                onChange={(e) =>
                                                    setNewMetadata((prev) =>
                                                        prev.map((f, j) =>
                                                            j === i
                                                                ? {
                                                                      ...f,
                                                                      value: e
                                                                          .target
                                                                          .value,
                                                                  }
                                                                : f,
                                                        ),
                                                    )
                                                }
                                            />
                                            <button
                                                type="button"
                                                className="set-card-form__meta-remove"
                                                onClick={() =>
                                                    setNewMetadata((prev) =>
                                                        prev.filter(
                                                            (_, j) => j !== i,
                                                        ),
                                                    )
                                                }
                                            >
                                                ×
                                            </button>
                                        </div>
                                    ))}

                                    <button
                                        type="button"
                                        className="set-card-form__meta-add"
                                        onClick={() =>
                                            setNewMetadata((prev) => [
                                                ...prev,
                                                { key: "", value: "" },
                                            ])
                                        }
                                    >
                                        + Add metadata field
                                    </button>

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
