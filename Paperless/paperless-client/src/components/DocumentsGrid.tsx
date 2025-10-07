import type { DocumentDto } from "../dto/documentDto";
import { DocumentCard } from "./DocumentCard";

interface Props {
    documents: DocumentDto[];
    onDelete?: (id: string) => void;
    onSelect?: (id: string) => void;
}

export function DocumentsGrid({ documents, onDelete, onSelect }: Props) {
    return(
        <div className="documents-grid">
            { documents.length === 0 ? (
                <p>No documents found.</p>
            ) : (
                documents.map (doc => (
                    <DocumentCard key={doc.id} document={doc} onDelete={onDelete} onSelect={onSelect}/>
                ))
            )}
        </div>
    );
}