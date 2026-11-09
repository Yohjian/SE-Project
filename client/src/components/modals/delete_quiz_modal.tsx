import Modal from "./modal";

type DeleteQuizModalProps = {
    quizTitle?: string;
    onCancel: () => void;
    onConfirm: () => void;
};

export default function DeleteQuizModal({
    quizTitle,
    onCancel,
    onConfirm,
}: DeleteQuizModalProps) {
    return (
        <Modal title="Delete quiz?" onClose={onCancel}>
            <p className="modal-text" style={{ marginBottom: 0 }}>
                {quizTitle ? <strong>“{quizTitle}”</strong> : "This quiz"} and
                all its questions will be permanently deleted. This can’t be
                undone.
            </p>

            <div className="modal-actions">
                <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={onCancel}
                >
                    Cancel
                </button>
                <button
                    type="button"
                    className="btn btn-danger"
                    onClick={onConfirm}
                >
                    Delete quiz
                </button>
            </div>
        </Modal>
    );
}
