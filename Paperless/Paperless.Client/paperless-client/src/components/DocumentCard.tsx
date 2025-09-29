import type { DocumentDto } from "../dto/DocumentDto";

interface Props {
    document: DocumentDto;
    onDelete?: (id: string) => void;
}

export function DocumentCard({ document, onDelete }: Props) {
    return (
        <div className="document-card">
            <h3>{document.name}</h3>
            <p className="document-content">{document.content}</p>
            {onDelete && (
                <button onClick={() => onDelete(document.id)}>Delete</button>
            )}
        </div>
    );
}