import { useState } from "react";
import "../styles/tree.css";

interface Item {
  id: number;
  name: string;
}

interface TreeProperties {
  title: string;
  entries: Item[];
  selectedId: number | null;
  onSelect: (id: number) => void;
  onAdd?: (name: string) => void;
}

export default function Tree({
  title,
  entries,
  selectedId,
  onSelect,
  onAdd,
}: TreeProperties) {
  const [isAddingSet, setIsAddingSet] = useState(false);
  const [newSetName, setNewSetName] = useState("");

  const handleConfirm = () => {
    if (!newSetName.trim()) return;
    onAdd?.(newSetName.trim());
    setNewSetName("");
    setIsAddingSet(false);
  };

  return (
    <aside className="sidebar">
      <div className="sidebar-header">
        <h3 className="sidebar-title">{title}</h3>
        {onAdd && (
          <button
            className="sidebar-add-btn"
            onClick={() => setIsAddingSet(true)}
          >
            +
          </button>
        )}
      </div>
      <ul className="sidebar-list">
        {entries.map((item) => (
          <li
            key={item.id}
            className={`sidebar-item ${item.id === selectedId ? "active" : ""}`}
            onClick={() => onSelect(item.id)}
          >
            {item.name}
          </li>
        ))}
      </ul>
      {isAddingSet && (
        <div className="sidebar-new-set">
          <input
            autoFocus
            type="text"
            placeholder="Set name..."
            value={newSetName}
            onChange={(e) => setNewSetName(e.target.value)}
            onKeyDown={(e) => {
              switch (e.key) {
                case "Enter":
                  handleConfirm();
                  break;
                case "Escape":
                  setIsAddingSet(false);
                  setNewSetName("");
                  break;
              }
            }}
          />
        </div>
      )}
    </aside>
  );
}
