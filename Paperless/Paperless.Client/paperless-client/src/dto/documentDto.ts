export interface DocumentDto {
  id: string;
  name: string;
  content: string;
  summary: string;
  creationDate: string;
  type: string;
  size: number;
}

export interface CreateDocumentDto {
  name: string;
  content: string;
}