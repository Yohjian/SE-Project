import { useEffect, useId, type ReactNode } from "react";

import "../../styles/modal.css";

type ModalProps = {
    title: string;
    onClose: () => void;
    /** Blocks Escape / overlay-click closing, e.g. while a request is running. */
    closeDisabled?: boolean;
    children: ReactNode;
};

export default function Modal({
    title,
    onClose,
    closeDisabled = false,
    children,
}: ModalProps) {
    const titleId = useId();

    useEffect(() => {
        const handleKeyDown = (event: KeyboardEvent) => {
            if (event.key === "Escape" && !closeDisabled) onClose();
        };

        document.addEventListener("keydown", handleKeyDown);
        return () => document.removeEventListener("keydown", handleKeyDown);
    }, [onClose, closeDisabled]);

    return (
        <div
            className="modal-overlay"
            onMouseDown={(event) => {
                if (event.target === event.currentTarget && !closeDisabled)
                    onClose();
            }}
        >
            <div
                className="modal"
                role="dialog"
                aria-modal="true"
                aria-labelledby={titleId}
            >
                <h2 id={titleId} className="modal-title">
                    {title}
                </h2>
                {children}
            </div>
        </div>
    );
}
