import type { CreateDocumentDto } from "../dto/DocumentDto";

interface Props {
    onUploaded?: (document: CreateDocumentDto) => void;
}

export function UploadDocument({ onUploaded }: Props) {
    const dummyDocument: CreateDocumentDto = {
        name: `Document ${Math.floor(Math.random() * 100)}`, 
        content: `Lorem ipsum dolor sit amet.`
    }; 
    return(
        <button onClick={() => onUploaded?.(dummyDocument)}>
            Upload Document
        </button>
    );
}