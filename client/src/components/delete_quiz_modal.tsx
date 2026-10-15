
type DeleteQuizModalProps = {
  onCancel: () => void;
  onConfirm: () => void;
};

export default function DeleteQuizModal({
  onCancel,
  onConfirm,
}: DeleteQuizModalProps) {
  return (
    <div className="delete-modal-overlay">
      <div
        className="delete-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="delete-modal-title"
      >
        <h2 id="delete-modal-title">Delete quiz?</h2>

        <p>
          Are you sure you want to delete this quiz?
          This action cannot be undone.
        </p>

        <div className="delete-modal-actions">
          <button
            type="button"
            onClick={onCancel}
          >
            Cancel
          </button>

          <button
            type="button"
            className="confirm-delete-button"
            onClick={onConfirm}
          >
            Delete quiz
          </button>
        </div>
      </div>
    </div>
  );
}
