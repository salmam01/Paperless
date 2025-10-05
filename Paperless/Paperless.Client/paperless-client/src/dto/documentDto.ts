export interface DocumentDto {
  id: string;
  name: string;
  content: string;
  summary: string;
  filePath: string; // temporary
  creationDate: string;
  type: string;
  size: number;
}