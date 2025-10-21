import type { DocumentDto } from "../dto/documentDto";

interface Props {
    document: DocumentDto;
    onDelete?: (id: string) => void;
    onSelect?: (id: string) => void;
}

export function DocumentCard({ document, onDelete, onSelect }: Props) {
    const hasActions = onDelete || onSelect;
    
    return (
        <div className={`document-card ${hasActions ? 'has-actions' : ''}`}>
            <div className="document-header">
                <h3>{document.name}</h3>
                <span className="document-type">{document.type}</span>
            </div>
            <p className="document-content">{document.content}</p><br/>
            <div className="document-metadata">
                <span className="document-size">{Math.round(document.size * 100) / 100} MB</span>
                <span className="document-date">{new Date(document.creationDate).toLocaleDateString('de-DE')}</span>
                <br/></div><br/>
            {onSelect && (
                <button
                    className="card-action-button card-action-button--info"
                    aria-label="Open details"
                    title="Details"
                    onClick={(e) => { e.stopPropagation(); onSelect(document.id); }}
                >
                    <svg
                        xmlns="http://www.w3.org/2000/svg"
                        width="20"
                        height="20"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="2"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                    >
                        <circle cx="12" cy="12" r="10" />
                        <line x1="12" y1="16" x2="12" y2="12" />
                        <line x1="12" y1="8" x2="12.01" y2="8" />
                    </svg>
                </button>
            )}
            {onDelete && (
                <button
                    className="card-action-button card-action-button--delete"
                    aria-label="Delete document"
                    title="Delete"
                    onClick={(e) => { e.stopPropagation(); onDelete(document.id); }}
                >
                    <svg
                        xmlns="http://www.w3.org/2000/svg"
                        width="20"
                        height="20"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="2"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                    >
                        <polyline points="3 6 5 6 21 6" />
                        <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                        <path d="M10 11v6" />
                        <path d="M14 11v6" />
                        <path d="M9 6V4a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v2" />
                    </svg>
                </button>
            )}
        </div>
    );
}