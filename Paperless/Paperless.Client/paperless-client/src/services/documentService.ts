import type { DocumentDto } from "../dto/documentDto";

const apiUrl = 'http://localhost:8080/api/Document'

export async function getDocuments(): Promise<DocumentDto[]> {
    const response = await fetch(apiUrl)

    if (!response.ok) {
        throw new Error('Failed to fetch documents');
    }

    return await response.json();
}

export async function getDocument(id: string): Promise<DocumentDto> {
    const response = await fetch(`${apiUrl}/${id}`)

    if (!response.ok) {
        throw new Error(`Failed to fetch document with ID: ${id}`);
    }

    return await response.json();
}


export async function postDocument(document: DocumentDto): Promise<DocumentDto> {
    const response = await fetch(apiUrl, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(document),
    });

    if (!response.ok) {
        throw new Error('Failed to create document');
    }

    return await response.json();
}

export async function deleteDocuments(): Promise<void> {
    const response = await fetch(apiUrl, {
        method: 'DELETE'
    });

    if (!response.ok) throw new Error('Failed to delete documents')
}

export async function deleteDocument(id: string): Promise<void> {
    const response = await fetch(`${apiUrl}/${id}`, {
        method: 'DELETE'
    });

    if (!response.ok) throw new Error(`Failed to delete document with ID: ${id}`)
}