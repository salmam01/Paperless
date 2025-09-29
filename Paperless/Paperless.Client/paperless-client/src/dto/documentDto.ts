export interface DocumentDto {
  id: string;
  name: string;
  content: string;
}

export interface CreateDocumentDto {
  name: string;
  content: string;
}