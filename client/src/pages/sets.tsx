import { useState, useEffect, useRef } from "react";
import { createPortal } from "react-dom";
import Tree from "../components/tree";
import { cardApi, setApi } from "../api/client";

import "../styles/sets.css";
import { useNotification } from "../components/notification";
import { CardResponse, SetSummary } from "../api/types";

export default function Sets() {
  const [selectedSetID, setSelectedSetID] = useState<number | null>(null);
  const [viewMode, setViewMode] = useState<"grid" | "list">("grid");
  const [search, setSearch] = useState("");
  const [isAdding, setIsAdding] = useState(false);
  const [newTerm, setNewTerm] = useState("");
  const [newDefinition, setNewDefinition] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [sets, setSets] = useState<SetSummary[]>([]);
  const [cards, setCards] = useState<CardResponse[]>([]);

  const termRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (isAdding) {
      termRef.current?.focus();
    }
  }, [isAdding]);

  // init load
  useEffect(() => {
    setApi
      .getAll()
      .then((res) => setSets(res.data))
      .catch(() => showNotification("Failed to load sets."));
  }, []);

  // on set change
  useEffect(() => {
    if (selectedSetID === null) return;
    setApi
      .getCards(selectedSetID)
      .then((res) => setCards(res.data))
      .catch(() => showNotification("Failed to load cards."));
  }, [selectedSetID]);

  const filteredCards = cards.filter(
    (c) =>
      c.term.toLowerCase().includes(search.toLowerCase()) ||
      c.definition.toLowerCase().includes(search.toLowerCase()),
  );

  const { showNotification } = useNotification();

  const handleSave = async () => {
    if (!newTerm.trim()) {
      showNotification("A term is needed!");
      return;
    }
    if (!newDefinition.trim()) {
      showNotification("A definition is needed!");
      return;
    }
    if (selectedSetID === null) {
      showNotification("No set selected.");
      return;
    }

    setIsSaving(true);

    try {
      const res = await cardApi.add(selectedSetID, newTerm, newDefinition);
      setCards((prev) => [...prev, res.data]);
      showNotification("Card added.", "success");
    } catch (err: unknown) {
      const message =
        err instanceof Error ? err.message : "Failed to add card.";
      showNotification(message);
    } finally {
      handleCancel();
      setIsSaving(false);
    }
  };

  const handleCancel = async () => {
    setIsAdding(false);
    setNewTerm("");
    setNewDefinition("");
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    switch (e.key) {
      case "Enter":
        handleSave();
        break;
      case "Escape":
        handleCancel();
        break;
    }
  };

  const handleAddSet = async (name: string) => {
    try {
      const res = await setApi.create(name);
      setSets((prev) => [...prev, res.data]);
      setSelectedSetID(res.data.id);
    } catch {
      showNotification("Failed to create set.");
    }
  };

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
          selectedId={selectedSetID}
          onSelect={setSelectedSetID}
          onAdd={handleAddSet}
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
              onClick={() => setViewMode(viewMode === "grid" ? "list" : "grid")}
            >
              {viewMode === "grid" ? "List view" : "Grid view"}
            </button>
          </div>

          <div className={viewMode === "grid" ? "cards-grid" : "cards-list"}>
            {filteredCards.map((card) => (
              <div
                key={card.id}
                className={`card ${viewMode === "list" ? "card-list" : ""}`}
              >
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
                  onChange={(e) => setNewDefinition(e.target.value)}
                  onKeyDown={handleKeyDown}
                />
                <div className="card-add-form-actions">
                  <button onClick={handleCancel} disabled={isSaving}>
                    Cancel
                  </button>
                  <button onClick={handleSave} disabled={isSaving}>
                    Save
                  </button>
                </div>
              </div>
            ) : (
              <div className="card card-add" onClick={() => setIsAdding(true)}>
                <span>+ Add card</span>
              </div>
            )}
          </div>
        </div>
      </div>
    </>
  );
}
