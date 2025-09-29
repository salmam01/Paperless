import type { DocumentDto } from "../dto/documentDto";
import { DocumentCard } from "./DocumentCard";

interface Props {
    documents: DocumentDto[];
    onDelete?: (id: string) => void;
    onDeleteAll?: () => void;
}

export function DocumentsGrid({ documents, onDelete, onDeleteAll }: Props) {
    return(
        <div className="documents-grid">
            {documents.map (doc => (
                <DocumentCard key={doc.id} document={doc} onDelete={onDelete}/>
            ))}
            <button onClick={() => onDeleteAll?.()}>Delete All</button>
        </div>
    );
}