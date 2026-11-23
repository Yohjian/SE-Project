
import {
    useState,
    useEffect,
    useRef,
    useMemo,
    useCallback,
} from "react";
import { createPortal } from "react-dom";
import axios from "axios";

import { cardApi, setApi, setFolderApi } from "../api/client";
import type {
    CardResponse,
    SetFolder,
    SetSummary,
} from "../api/types";
import { useNotification } from "../components/notification";

import "../styles/sets.css";
import "../styles/tree.css";

function filenameFromHeader(
    header: string | undefined,
    fallback: string,
) {
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

    setTimeout(() => URL.revokeObjectURL(url), 0);
}

function errorMessage(error: unknown, fallback: string) {
    if (!axios.isAxiosError(error)) return fallback;

    const data = error.response?.data;

    if (typeof data === "string") return data;

    return data?.detail ?? data?.title ?? fallback;
}

export default function Sets() {
    const [sets, setSets] = useState<SetSummary[]>([]);
    const [folders, setFolders] = useState<SetFolder[]>([]);

    const [selectedFolderId, setSelectedFolderId] =
        useState<number | null>(null);
    const [selectedSetId, setSelectedSetId] =
        useState<number | null>(null);

    const [expandedFolders, setExpandedFolders] =
        useState<Set<number>>(new Set());

    const [newFolderName, setNewFolderName] = useState("");
    const [isAddingFolder, setIsAddingFolder] = useState(false);
    const [isSavingFolder, setIsSavingFolder] = useState(false);

    const [renamingFolder, setRenamingFolder] = useState<SetFolder | null>(null);
    const [renameValue, setRenameValue] = useState("");
    const [isRenamingFolder, setIsRenamingFolder] = useState(false);

    const [newSetName, setNewSetName] = useState("");
    const [isAddingSet, setIsAddingSet] = useState(false);
    const [isSavingSet, setIsSavingSet] = useState(false);
    const [newSetFolderId, setNewSetFolderId] = useState<number | null>(null);

    const [viewMode, setViewMode] =
        useState<"grid" | "list">("grid");

    const [search, setSearch] = useState("");
    const [isAdding, setIsAdding] = useState(false);
    const [newTerm, setNewTerm] = useState("");
    const [newDefinition, setNewDefinition] = useState("");
    const [newMetadata, setNewMetadata] = useState<MetaField[]>([]);
    const [isSaving, setIsSaving] = useState(false);
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
                (card) =>
                    card.term
                        .toLowerCase()
                        .includes(search.toLowerCase()) ||
                    card.definition
                        .toLowerCase()
                        .includes(search.toLowerCase()),
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

    const selectedSet = useMemo(
        () => sets.find((set) => set.id === selectedSetId),
        [sets, selectedSetId],
    );

    const setsInFolder = (folderId: number) => {
        return sets.filter((set) => set.folderId === folderId);
    };

    useEffect(() => {
        if (isAdding) termRef.current?.focus();
    }, [isAdding]);

    useEffect(() => {
        const handler = (event: MouseEvent) => {
            if (
                exportRef.current &&
                !exportRef.current.contains(event.target as Node)
            ) {
                setShowExportMenu(false);
            if (sortRef.current && !sortRef.current.contains(e.target as Node))
                setShowSortMenu(false);
            }
        };

        document.addEventListener("mousedown", handler);

        return () =>
            document.removeEventListener("mousedown", handler);
    }, []);

    useEffect(() => {
        let active = true;

        Promise.all([setApi.getAll(), setFolderApi.getAll()])
            .then(([setResponse, folderResponse]) => {
                if (!active) return;

                setSets(setResponse.data);
                setFolders(folderResponse.data);
            })
            .catch(() => {
                if (active)
                    showNotification("Failed to load sets or folders.");
            });

        return () => {
            active = false;
        };
    }, [showNotification]);

    useEffect(() => {
        if (selectedSetId === null) {
            setCards([]);
            return;
        }

        let active = true;
        setCards([]);

        setApi
            .getCards(selectedSetId)
            .then((res) => {
                if (active) setCards(res.data);
            })
            .catch(() => {
                if (active)
                    showNotification("Failed to load cards.");
            });

        return () => {
            active = false;
        };
    }, [selectedSetId, showNotification]);

    const selectFolder = (folderId: number | null) => {
        setSelectedFolderId(folderId);
        setSelectedSetId(null);
        setIsAdding(false);
        setSearch("");
    };

    const toggleFolder = (folderId: number) => {
        setExpandedFolders((previous) => {
            const next = new Set(previous);

            if (next.has(folderId)) {
                next.delete(folderId);
            } else {
                next.add(folderId);
            }

            return next;
        });
    };

    const selectSet = (set: SetSummary) => {
        setSelectedSetId(set.id);
        setSelectedFolderId(set.folderId ?? null);
        setIsAdding(false);
        setSearch("");
    };

    const handleCreateFolder = async () => {
        const name = newFolderName.trim();

        if (!name || isSavingFolder) return;

        setIsSavingFolder(true);

        try {
            const response = await setFolderApi.create(name);

            setFolders((previous) => [
                ...previous,
                response.data,
            ]);

            setExpandedFolders((previous) => {
                const next = new Set(previous);
                next.add(response.data.id);
                return next;
            });

            selectFolder(response.data.id);
            setNewFolderName("");
            setIsAddingFolder(false);

            showNotification("Folder created.", "success");
        } catch (error) {
            showNotification(
                errorMessage(error, "Failed to create folder."),
            );
        } finally {
            setIsSavingFolder(false);
        }
    };

    const handleRenameFolder = async () => {
        if (!renamingFolder || isRenamingFolder) return;

        const name = renameValue.trim();

        if (!name) {
            showNotification("Folder name cannot be empty.");
            return;
        }

        if (name === renamingFolder.name) {
            setRenamingFolder(null);
            return;
        }

        setIsRenamingFolder(true);

        try {
            const response = await setFolderApi.rename(
                renamingFolder.id,
                name,
            );

            setFolders((previous) =>
                previous.map((item) =>
                    item.id === renamingFolder.id ? response.data : item,
                ),
            );

            setRenamingFolder(null);
            showNotification("Folder renamed.", "success");
        } catch (error) {
            showNotification(
                errorMessage(error, "Failed to rename folder."),
            );
        } finally {
            setIsRenamingFolder(false);
        }
    };

    const handleDeleteFolder = async (folder: SetFolder) => {
        try {
            await setFolderApi.delete(folder.id);

            setFolders((previous) =>
                previous.filter((item) => item.id !== folder.id),
            );

            const deletedSetIds = new Set(
                sets
                    .filter((set) => set.folderId === folder.id)
                    .map((set) => set.id),
            );

            setSets((previous) =>
                previous.filter((set) => !deletedSetIds.has(set.id)),
            );

            setExpandedFolders((previous) => {
                const next = new Set(previous);
                next.delete(folder.id);
                return next;
            });

            if (
                selectedFolderId === folder.id ||
                (selectedSetId !== null && deletedSetIds.has(selectedSetId))
            ) {
                setSelectedFolderId(null);
                setSelectedSetId(null);
                setCards([]);
            }

            showNotification(
                "Folder and all its sets and cards deleted.",
                "success",
            );
        } catch (error) {
            showNotification(
                errorMessage(error, "Failed to delete folder."),
            );
        }
    };

    const handleAddSet = async () => {
        const name = newSetName.trim();

        if (!name || isSavingSet) return;

        if (newSetFolderId === null) {
            showNotification("Select a folder before creating a set.");
            return;
        }

        setIsSavingSet(true);

        try {
            const response = await setApi.create(name, newSetFolderId);
            const created = response.data;

            setSets((previous) => [...previous, created]);

            setSelectedFolderId(newSetFolderId);
            setSelectedSetId(created.id);

            setExpandedFolders((previous) => {
                const next = new Set(previous);
                next.add(newSetFolderId);
                return next;
            });

            setIsAddingSet(false);
            setNewSetName("");
            setNewSetFolderId(null);

            showNotification("Set created.", "success");
        } catch (error) {
            showNotification(
                errorMessage(error, "Failed to create set."),
            );
        } finally {
            setIsSavingSet(false);
        }
    };

    const handleDeleteSet = async (setId: number) => {
        if (!confirm("Delete this set and all its cards?")) return;

        try {
            await setApi.delete(setId);

            setSets((previous) =>
                previous.filter((set) => set.id !== setId),
            );

            if (selectedSetId === setId) {
                setSelectedSetId(null);
                setCards([]);
            }

            showNotification("Set deleted.", "success");
        } catch {
            showNotification("Failed to delete set.");
        }
    };

    const handleMoveSet = async (
        setId: number,
        folderId: number | null,
    ) => {
        try {
            await setApi.moveToFolder(setId, folderId);

            setSets((previous) =>
                previous.map((set) =>
                    set.id === setId
                        ? { ...set, folderId }
                        : set,
                ),
            );

            if (selectedSetId === setId) {
                setSelectedFolderId(folderId);
            }

            showNotification("Set moved.", "success");
        } catch (error) {
            showNotification(
                errorMessage(error, "Failed to move set."),
            );
        }
    };

    const handleCancel = useCallback(() => {
        setIsAdding(false);
        setNewTerm("");
        setNewDefinition("");
        setNewMetadata([]);
    }, []);

    const handleSave = useCallback(async () => {
        if (!newTerm.trim())
            return showNotification("A term is needed!");

        if (!newDefinition.trim())
            return showNotification("A definition is needed!");

        if (selectedSetId === null)
            return showNotification("No set selected.");

        const metadata = Object.fromEntries(
            newMetadata
                .filter((f) => f.key.trim())
                .map((f) => [f.key.trim(), parseMetaValue(f.value)]),
        );

        setIsSaving(true);

        try {
            const response = await cardApi.add(
                selectedSetId,
                newTerm,
                newDefinition,
                Object.keys(metadata).length > 0 ? metadata : undefined,
            );

            setCards((previous) => [
                ...previous,
                response.data,
            ]);

            setSets((previous) =>
                previous.map((set) =>
                    set.id === selectedSetId
                        ? { ...set, cardCount: set.cardCount + 1 }
                        : set,
                ),
            );

            showNotification("Card added.", "success");
            handleCancel();
        } catch (error) {
            showNotification(
                errorMessage(error, "Failed to add card."),
            );
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

                setCards((previous) =>
                    previous.filter((card) => card.id !== cardId),
                );

                setSets((previous) =>
                    previous.map((set) =>
                        set.id === selectedSetId
                            ? {
                                  ...set,
                                  cardCount: Math.max(
                                      0,
                                      set.cardCount - 1,
                                  ),
                              }
                            : set,
                    ),
                );

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
            if (selectedSetId === null) return;

            try {
                const response = await setApi.download(
                    selectedSetId,
                    format,
                );

                const filename = filenameFromHeader(
                    response.headers["content-disposition"],
                    `set.${format}`,
                );

                downloadBlob(response.data, filename);
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
              <div
                  className="sets-overlay"
                  onClick={handleCancel}
              />,
              document.body,
          )
        : null;

    const renderSet = (set: SetSummary) => (
        <li
            key={set.id}
            className={`sidebar-item ${
                selectedSetId === set.id ? "active" : ""
            }`}
        >
            <button
                type="button"
                className="sidebar-item-name"
                onClick={() => selectSet(set)}
                aria-current={
                    selectedSetId === set.id ? "true" : undefined
                }
            >
                {set.name}
            </button>

            <button
                type="button"
                className="sidebar-item-delete"
                onClick={() => handleDeleteSet(set.id)}
                title={`Delete ${set.name}`}
                aria-label={`Delete ${set.name}`}
            >
                ×
            </button>
        </li>
    );

    return (
        <>
            {overlay}
            {isAddingSet &&
    createPortal(
        <div
            className="folder-modal-overlay"
            onMouseDown={(event) => {
                if (
                    event.target === event.currentTarget &&
                    !isSavingSet
                ) {
                    setIsAddingSet(false);
                }
            }}
        >
            <div
                className="folder-modal"
                role="dialog"
                aria-modal="true"
                aria-labelledby="create-set-title"
            >
                <h2 id="create-set-title">Create new set</h2>

                <form
                    onSubmit={(event) => {
                        event.preventDefault();
                        void handleAddSet();
                    }}
                >
                    <label htmlFor="new-set-name">
                        Set name
                    </label>

                    <input
                        id="new-set-name"
                        autoFocus
                        className="input"
                        type="text"
                        placeholder="Enter set name..."
                        value={newSetName}
                        onChange={(event) =>
                            setNewSetName(event.target.value)
                        }
                        maxLength={100}
                        disabled={isSavingSet}
                    />

                    <label htmlFor="new-set-folder">
                        Folder
                    </label>

                    <select
                        id="new-set-folder"
                        className="input"
                        value={newSetFolderId ?? ""}
                        onChange={(event) =>
                            setNewSetFolderId(
                                event.target.value
                                    ? Number(event.target.value)
                                    : null
                            )
                        }
                        disabled={isSavingSet}
                    >
                        <option value="" disabled>
                            Select a folder
                        </option>

                        {folders.map((folder) => (
                            <option
                                key={folder.id}
                                value={folder.id}
                            >
                                {folder.name}
                            </option>
                        ))}
                    </select>

                    <div className="folder-modal-actions">
                        <button
                            type="button"
                            className="btn btn-secondary"
                            onClick={() => setIsAddingSet(false)}
                            disabled={isSavingSet}
                        >
                            Cancel
                        </button>

                        <button
                            type="submit"
                            className="btn btn-primary"
                            disabled={
                                isSavingSet ||
                                !newSetName.trim() ||
                                newSetFolderId === null
                            }
                        >
                            {isSavingSet
                                ? "Creating..."
                                : "Create set"}
                        </button>
                    </div>
                </form>
            </div>
        </div>,
        document.body,
    )}
            {renamingFolder &&
    createPortal(
        <div
            className="folder-modal-overlay"
            onMouseDown={(event) => {
                if (
                    event.target === event.currentTarget &&
                    !isRenamingFolder
                ) {
                    setRenamingFolder(null);
                }
            }}
        >
            <div
                className="folder-modal"
                role="dialog"
                aria-modal="true"
                aria-labelledby="rename-folder-title"
            >
                <h2 id="rename-folder-title">Rename folder</h2>

                <form
                    onSubmit={(event) => {
                        event.preventDefault();
                        void handleRenameFolder();
                    }}
                >
                    <input
                        autoFocus
                        className="input"
                        type="text"
                        value={renameValue}
                        onChange={(event) =>
                            setRenameValue(event.target.value)
                        }
                        placeholder="Folder name"
                        maxLength={100}
                        disabled={isRenamingFolder}
                        onKeyDown={(event) => {
                            if (
                                event.key === "Escape" &&
                                !isRenamingFolder
                            ) {
                                setRenamingFolder(null);
                            }
                        }}
                    />

                    <div className="folder-modal-actions">
                        <button
                            type="button"
                            className="btn btn-secondary"
                            onClick={() => setRenamingFolder(null)}
                            disabled={isRenamingFolder}
                        >
                            Cancel
                        </button>

                        <button
                            type="submit"
                            className="btn btn-primary"
                            disabled={
                                isRenamingFolder ||
                                !renameValue.trim()
                            }
                        >
                            {isRenamingFolder ? "Saving..." : "Save"}
                        </button>
                    </div>
                </form>
            </div>
        </div>,
        document.body,
    )}

            <div className="sets-page">
                <aside className="sidebar sets-folder-sidebar">
                    <div className="sidebar-header sets-sidebar-header">
                        <h3 className="sidebar-title">Sets</h3>
                    </div>

                    <div className="sets-sidebar-actions">
                        <button
                            type="button"
                            className="btn btn-primary btn-sm"
                            onClick={() => {
                                setIsAddingFolder((value) => !value);
                                setIsAddingSet(false);
                            }}
                        >
                            + Add folder
                        </button>

                        <button
                            type="button"
                            className="btn btn-primary btn-sm"
                            onClick={() => {
                                setNewSetFolderId(
                                    selectedFolderId ?? folders[0]?.id ?? null
                                );
                                setNewSetName("");
                                setIsAddingSet(true);
                                setIsAddingFolder(false);
                            }}
                        >
                            + Add set
                        </button>
                    </div>

                    {isAddingFolder && (
                        <div className="sidebar-new-set">
                            <input
                                className="input"
                                type="text"
                                placeholder="Folder name..."
                                aria-label="New folder name"
                                value={newFolderName}
                                onChange={(event) =>
                                    setNewFolderName(event.target.value)
                                }
                                onKeyDown={(event) => {
                                    if (event.key === "Enter")
                                        void handleCreateFolder();

                                    if (event.key === "Escape")
                                        setIsAddingFolder(false);
                                }}
                            />

                            <button
                                type="button"
                                className="btn btn-primary btn-sm"
                                onClick={handleCreateFolder}
                                disabled={isSavingFolder}
                            >
                                Add
                            </button>
                        </div>
                    )}

                    <div className="sets-folder-scroll">
                        {folders.map((folder) => {
                            const expanded = expandedFolders.has(folder.id);

                            return (
                                <div
                                    key={folder.id}
                                    className="sets-folder-group"
                                >
                                    <div className="sets-folder-row">
                                        <button
                                            type="button"
                                            className={`sets-folder-heading ${
                                                selectedFolderId === folder.id
                                                    ? "sets-folder-heading--active"
                                                    : ""
                                            }`}
                                            onClick={() => {
                                                selectFolder(folder.id);
                                                toggleFolder(folder.id);
                                            }}
                                        >
                                            <span className="sets-folder-label" title={folder.name}>
                                                <span className="sets-folder-arrow">
                                                    {expanded ? "▾" : "▸"}
                                                </span>

                                                <span className="sets-folder-name">
                                                    {folder.name}
                                                </span>
                                            </span>
                                        </button>

                                        <button
                                            type="button"
                                            className="sets-folder-action"
                                            title={`Rename ${folder.name}`}
                                            aria-label={`Rename ${folder.name}`}
                                            onClick={() => {
                                                setRenamingFolder(folder);
                                                setRenameValue(folder.name);
                                            }}
                                        >
                                            ✎
                                        </button>

                                        <button
                                            type="button"
                                            className="sets-folder-action"
                                            title={`Delete ${folder.name}`}
                                            aria-label={`Delete ${folder.name}`}
                                            onClick={() =>
                                                handleDeleteFolder(folder)
                                            }
                                        >
                                            ×
                                        </button>
                                    </div>

                                    {expanded && (
                                        <ul className="sidebar-list sets-folder-sets">
                                            {setsInFolder(folder.id).map(
                                                renderSet,
                                            )}
                                        </ul>
                                    )}
                                </div>
                            );
                        })}
                    </div>
                </aside>

                <div className="sets-content">
                    <div className="sets-toolbar">
                        <input
                            type="text"
                            className="input sets-toolbar__search"
                            placeholder="Search cards..."
                            aria-label="Search cards"
                            value={search}
                            onChange={(event) =>
                                setSearch(event.target.value)
                            }
                            disabled={selectedSetId === null}
                        />

                        {selectedSet && (
                            <select
                                className="input sets-folder-select"
                                aria-label="Move set to folder"
                                value={selectedSet.folderId ?? ""}
                                onChange={(event) =>
                                    handleMoveSet(
                                        selectedSet.id,
                                        event.target.value
                                            ? Number(event.target.value)
                                            : null,
                                    )
                                }
                            >
                                <option value="" disabled>
                                    Select a folder
                                </option>

                                {folders.map((folder) => (
                                    <option
                                        key={folder.id}
                                        value={folder.id}
                                    >
                                        {folder.name}
                                    </option>
                                ))}
                            </select>
                        )}

                        <button
                            type="button"
                            className="btn btn-secondary btn-sm"
                            onClick={() =>
                                setViewMode((mode) =>
                                    mode === "grid" ? "list" : "grid",
                                )
                            }
                        >
                            {viewMode === "grid"
                                ? "List view"
                                : "Grid view"}
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
                        <div
                            className="sets-export"
                            ref={exportRef}
                        >
                            <button
                                type="button"
                                className="btn btn-secondary btn-sm"
                                onClick={() =>
                                    setShowExportMenu((value) => !value)
                                }
                                disabled={selectedSetId === null}
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
                                        onClick={() =>
                                            handleExport("json")
                                        }
                                    >
                                        JSON
                                    </button>

                                    <button
                                        type="button"
                                        className="sets-export__item"
                                        onClick={() =>
                                            handleExport("csv")
                                        }
                                    >
                                        CSV
                                    </button>
                                </div>
                            )}
                        </div>
                    </div>

                    {selectedSetId === null ? (
                        <p className="sets-empty text-muted">
                            Select a set from a folder or create a new set.
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
                                        onChange={(event) =>
                                            setNewTerm(event.target.value)
                                        }
                                        onKeyDown={(event) => {
                                            if (event.key === "Enter")
                                                void handleSave();

                                            if (event.key === "Escape")
                                                handleCancel();
                                        }}
                                    />

                                    <input
                                        className="input"
                                        type="text"
                                        placeholder="Definition"
                                        value={newDefinition}
                                        onChange={(event) =>
                                            setNewDefinition(
                                                event.target.value,
                                            )
                                        }
                                        onKeyDown={(event) => {
                                            if (event.key === "Enter")
                                                void handleSave();

                                            if (event.key === "Escape")
                                                handleCancel();
                                        }}
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
