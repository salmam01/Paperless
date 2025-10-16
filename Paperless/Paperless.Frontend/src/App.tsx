import { useState, useEffect } from 'react'
import './App.css'
import type { DocumentDto } from './dto/documentDto'
import { getDocuments, getDocument, deleteDocument, deleteDocuments, postDocument } from './services/documentService'
import { DocumentsGrid } from './components/DocumentsGrid'
import { DocumentDetails } from './components/DocumentDetails'
import { UploadPanel } from './components/UploadPanel'

function App() {
  const [documents, setDocuments] = useState<DocumentDto[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [selectedDocument, setSelectedDocument] = useState<DocumentDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showUploadPanel, setShowUploadPanel] = useState(false);

  useEffect(() => {
    fetchDocuments();
  }, []);

  const fetchDocuments = async () => {
    try {
      setLoading(true);
      const data = await getDocuments();
      setDocuments(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  const fetchDocument = async (id: string) => {
    try {
      setLoading(true);
      const doc = await getDocument(id);
      setSelectedDocument(doc);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  const handleSelect = (id: string) => {
    setSelectedId(id);
    setShowUploadPanel(false);
    fetchDocument(id);
  };

  const handleBack = () => {
    setSelectedId(null);
    setSelectedDocument(null);
    setShowUploadPanel(false);
  };

  const handleShowUpload = () => {
    setSelectedId(null);
    setSelectedDocument(null);
    setShowUploadPanel(true);
  };

  const handleDelete = async (id: string) => {
    try {
      await deleteDocument(id);
      if (selectedId === id) handleBack();
      fetchDocuments();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    }
  };

  const handleUpload = async (file: File) => {
    try {
      if (!file) return;

      setLoading(true);
      const formData = new FormData();
      formData.append("form", file);

      await postDocument(formData);
      await fetchDocuments();
      setShowUploadPanel(false);

    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred while uploading document.');
    }
    finally {
      setLoading(false);
    }
  };

  const handleDeleteAll = async () => {
    try {
      await deleteDocuments();
      handleBack();
      await fetchDocuments();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
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
            <div className="documents-toolbar">
              <button onClick={handleShowUpload}>Upload</button>
              <button onClick={handleDeleteAll}>Delete All</button>
            </div>
            <div className={`documents-layout ${(selectedDocument || showUploadPanel) ? 'with-panel' : ''}`}>
              <div className="documents-list">
                <DocumentsGrid documents={documents} onDelete={handleDelete} onSelect={handleSelect} />
              </div>
              {selectedDocument && (
                <aside className="details-panel">
                  <DocumentDetails document={selectedDocument} onBack={handleBack} />
                </aside>
              )}
              {showUploadPanel && (
                <aside className="details-panel">
                  <UploadPanel loading={loading} onUploaded={handleUpload} onBack={handleBack} />
                </aside>
              )}
            </div>
          </div>
        )}

          
        <div className="api-status">
          <button onClick={fetchDocuments} disabled={loading}>
            {loading ? 'Refreshing...' : 'Refresh Documents'}
          </button>
        </div>
      </main>
    </div>
  )
}

export default App
