import { useState, useEffect, useRef } from "react";
import "../styles/tree.css";

interface Entry {
    id: number;
    name: string;
}

interface TreeProps {
    title: string;
    entries: Entry[];
    selectedId: number | null;
    onSelect: (id: number) => void;
    onAdd?: (name: string) => void;
    onDelete?: (id: number) => void;
}

export default function Tree({
    title,
    entries,
    selectedId,
    onSelect,
    onAdd,
    onDelete,
}: TreeProps) {
    const [isAdding, setIsAdding] = useState(false);
    const [newName, setNewName] = useState("");
    const inputRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        if (isAdding) inputRef.current?.focus();
    }, [isAdding]);

    const handleAdd = () => {
        const name = newName.trim();
        if (name) onAdd?.(name);
        setNewName("");
        setIsAdding(false);
    };

    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === "Enter") handleAdd();
        if (e.key === "Escape") {
            setNewName("");
            setIsAdding(false);
        }
    };

    return (
        <div className="sidebar">
            <div className="sidebar-header">
                <span className="sidebar-title">{title}</span>
                {onAdd && (
                    <button
                        className="sidebar-add-btn"
                        onClick={() => setIsAdding(true)}
                    >
                        +
                    </button>
                )}
            </div>

            <ul className="sidebar-list">
                {entries.map((entry) => (
                    <li
                        key={entry.id}
                        className={`sidebar-item ${entry.id === selectedId ? "active" : ""}`}
                        onClick={() => onSelect(entry.id)}
                    >
                        <span className="sidebar-item-name">{entry.name}</span>
                        {onDelete && (
                            <button
                                className="sidebar-item-delete"
                                onClick={(e) => {
                                    e.stopPropagation();
                                    onDelete(entry.id);
                                }}
                                title="Delete set"
                            >
                                ×
                            </button>
                        )}
                    </li>
                ))}
            </ul>

            {isAdding && (
                <div className="sidebar-new-set">
                    <input
                        ref={inputRef}
                        type="text"
                        placeholder="Set name…"
                        value={newName}
                        onChange={(e) => setNewName(e.target.value)}
                        onKeyDown={handleKeyDown}
                    />
                </div>
            )}
        </div>
    );
}
