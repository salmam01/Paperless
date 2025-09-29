import { useState, useEffect } from 'react'
import './App.css'
import { getDocuments } from "./services/documentService";

function App() {
  const [documents, setDocuments] = useState<Document[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchDocuments();
  }, []);

  const fetchDocuments = async () => {
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
            {documents.length === 0 ? (
              <p>No documents found.</p>
            ) : (
              <div className="documents-grid">
                {documents.map((doc) => (
                  <div key={doc.id} className="document-card">
                    <h3>{doc.title}</h3>
                    <p className="document-content">{doc.content}</p>
                    <p className="document-date">
                      Created: {new Date(doc.createdAt).toLocaleDateString()}
                    </p>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
        
        <div className="api-status">
          <h3>API Status</h3>
          <p>Backend API is running and accessible</p>
          <button onClick={fetchDocuments} disabled={loading}>
            {loading ? 'Refreshing...' : 'Refresh Documents'}
          </button>
        </div>
      </main>
    </div>
  )
}

export default App
