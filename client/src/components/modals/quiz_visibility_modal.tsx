import Modal from "./modal";

type QuizVisibilityModalProps = {
    isPublic: boolean;
    isSaving: boolean;
    onConfirm: () => void;
    onCancel: () => void;
};

export default function QuizVisibilityModal({
    isPublic,
    isSaving,
    onConfirm,
    onCancel,
}: QuizVisibilityModalProps) {
    return (
        <Modal
            title="Change quiz visibility?"
            onClose={onCancel}
            closeDisabled={isSaving}
        >
            <p className="modal-text">
                {isPublic
                    ? "Are you sure you want to make this quiz public?"
                    : "Are you sure you want to make this quiz private?"}
            </p>

            <div className="modal-actions">
                <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={onCancel}
                    disabled={isSaving}
                >
                    Cancel
                </button>
                <button
                    type="button"
                    className="btn btn-primary"
                    onClick={onConfirm}
                    disabled={isSaving}
                >
                    {isSaving ? "Saving..." : "Confirm"}
                </button>
            </div>
        </Modal>
    );
}