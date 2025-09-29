import type { CreateDocumentDto, DocumentDto } from "../dto/DocumentDto";
import { DocumentCard } from "./DocumentCard";
import { UploadDocument } from "./UploadDocument";

interface Props {
    documents: DocumentDto[];
    onUploaded?: (document: CreateDocumentDto) => void;
    onDelete?: (id: string) => void;
    onDeleteAll?: () => void;
}

export function DocumentsGrid({ documents, onUploaded, onDelete, onDeleteAll }: Props) {
    return(
        <div className="documents-grid">
            { documents.length === 0 ? (
                <p>No documents found.</p>
            ) : (
                documents.map (doc => (
                    <DocumentCard key={doc.id} document={doc} onDelete={onDelete}/>
                ))
            )}
            <button onClick={() => onDeleteAll?.()}>Delete All</button>
            <UploadDocument
                onUploaded={onUploaded}>
            </UploadDocument>
        </div>
    );
}