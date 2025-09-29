import { useState, useEffect } from 'react'
import './App.css'
import { deleteDocument, deleteDocuments, getDocuments, postDocument } from "./services/documentService";
import { DocumentsGrid } from './components/DocumentsGrid';
import type { DocumentDto } from './dto/DocumentDto';

function App() {
  const [documents, setDocuments] = useState<DocumentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getDocumentsHandler();

  }, []);

  const getDocumentsHandler = async () => {
    try {
      setLoading(true);
      setDocuments(await getDocuments());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="app">
      <header className="app-header">
        <h1>Paperless Dashboard</h1>
        <p>Document Management System</p>
      </header>
      
      <main className="app-main">
        {loading && <div className="loading">Loading documents...</div>}
        {error && <div className="error">Error: {error}</div>}
        
        {!loading && !error && (
          <div className="documents-section">
            <h2>Documents ({documents.length})</h2>

              <DocumentsGrid 
                documents={documents}
                onUploaded={async (document) => {
                  await postDocument(document)
                  await getDocumentsHandler();
                }}
                onDelete={async (id) => {
                  await deleteDocument(id);
                  setDocuments(prev => prev.filter(d => d.id !== id));
                }}
                onDeleteAll={async() => {
                  await deleteDocuments();
                  setDocuments([])
                }}
              />

          </div>
        )}
        
        <div className="api-status">
          <button onClick={getDocumentsHandler} disabled={loading}>
            {loading ? 'Refreshing...' : 'Refresh Documents'}
          </button>
        </div>
      </main>
    </div>
  )
}

export default App
