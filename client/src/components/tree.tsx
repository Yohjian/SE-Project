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
        <aside className="sidebar">
            <div className="sidebar-header">
                <h3 className="sidebar-title">{title}</h3>
                {onAdd && (
                    <button
                        className="sidebar-add-btn"
                        onClick={() => setIsAdding(true)}
                        title={`Add to ${title}`}
                        aria-label={`Add to ${title}`}
                    >
                        +
                    </button>
                )}
            </div>

            <ul className="sidebar-list">
                {entries.map((entry) => {
                    const isActive = entry.id === selectedId;
                    return (
                        <li
                            key={entry.id}
                            className={`sidebar-item ${isActive ? "active" : ""}`}
                        >
                            <button
                                className="sidebar-item-name"
                                onClick={() => onSelect(entry.id)}
                                aria-current={isActive ? "true" : undefined}
                            >
                                {entry.name}
                            </button>
                            {onDelete && (
                                <button
                                    className="sidebar-item-delete"
                                    onClick={() => onDelete(entry.id)}
                                    title={`Delete ${entry.name}`}
                                    aria-label={`Delete ${entry.name}`}
                                >
                                    ×
                                </button>
                            )}
                        </li>
                    );
                })}
            </ul>

            {isAdding && (
                <div className="sidebar-new-set">
                    <input
                        ref={inputRef}
                        type="text"
                        placeholder={`${title} name…`}
                        aria-label={`New ${title} name`}
                        value={newName}
                        onChange={(e) => setNewName(e.target.value)}
                        onKeyDown={handleKeyDown}
                    />
                </div>
            )}
        </aside>
    );
}
