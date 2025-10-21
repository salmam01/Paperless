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
  const [showUploadPanel, setShowUploadPanel] = useState(false);
  const [notifications, setNotifications] = useState<Array<{id: string, message: string, type: 'error' | 'success' | 'info'}>>([]);

  useEffect(() => {
    fetchDocuments();
  }, []);

  const addNotification = (message: string, type: 'error' | 'success' | 'info' = 'error') => {
    const id = Date.now().toString();
    setNotifications(prev => [...prev, { id, message, type }]);
    
    //  after 5 sec: autoremve
    setTimeout(() => {
      setNotifications(prev => prev.filter(n => n.id !== id));
    }, 5000);
  };

  const removeNotification = (id: string) => {
    setNotifications(prev => prev.filter(n => n.id !== id));
  };

  const fetchDocuments = async () => {
    try {
      setLoading(true);
      const data = await getDocuments();
      setDocuments(data);
      addNotification('Documents loaded successfully', 'success');
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load documents';
      addNotification(errorMessage, 'error');
      // ui- block problem :Don't block the UI - insteasshow empty state 
      setDocuments([]);
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
      const errorMessage = err instanceof Error ? err.message : 'Failed to load document';
      addNotification(errorMessage, 'error');
      setSelectedDocument(null);
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
      addNotification('Document deleted successfully', 'success');
      fetchDocuments();
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to delete document';
      addNotification(errorMessage, 'error');
    }
  };
//Später:hier cleanen nicht vergessen, da wiederholen sich die catch blöcke
  const handleUpload = async (file: File) => {
    try {
      if (!file) return;

      // Debug-Logging: für TXT-Dateien
      if (file.name.toLowerCase().endsWith('.txt')) {
        console.log('TXT-Upload gestartet:', {
          fileName: file.name,
          fileSize: file.size,
          fileType: file.type,
          lastModified: file.lastModified
        });
      }

      setLoading(true);
      const formData = new FormData();
      formData.append("form", file);

      await postDocument(formData);
      addNotification('Document uploaded successfully', 'success');
      await fetchDocuments();
      setShowUploadPanel(false);

    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to upload document';
      console.error('Upload-Fehler:', err);
      addNotification(errorMessage, 'error');
    }
    finally {
      setLoading(false);
    }
  };

  const handleDeleteAll = async () => {
    try {
      await deleteDocuments();
      addNotification('All documents deleted successfully', 'success');
      handleBack();
      await fetchDocuments();
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to delete all documents';
      addNotification(errorMessage, 'error');
    }
  };

  return (
    <div className="app">
      <header className="app-header">
        <h1>Paperless Dashboard</h1>
        <p>Document Management System</p>
      </header>
      
      {/* Notification System */}
      <div className="notifications-container">
        {notifications.map(notification => (
          <div 
            key={notification.id} 
            className={`notification notification-${notification.type}`}
            onClick={() => removeNotification(notification.id)}
          >
            <span>{notification.message}</span>
            <button className="notification-close">×</button>
          </div>
        ))}
      </div>
      
      <main className="app-main">
        {loading && <div className="loading">Loading documents...</div>}
        
        <div className="documents-section">
          <h2>Documents ({documents.length})</h2>
          <div className="documents-toolbar">
            <button onClick={handleShowUpload} disabled={loading}>Upload</button>
            <button onClick={handleDeleteAll} disabled={loading || documents.length === 0}>Delete All</button>
            <button onClick={fetchDocuments} disabled={loading}>
              {loading ? 'Refreshing...' : 'Refresh'}
            </button>
          </div>
          <div className={`documents-layout ${(selectedDocument || showUploadPanel) ? 'with-panel' : ''}`}>
            <div className="documents-list">
              {documents.length === 0 && !loading ? (
                <div className="empty-state">
                  <h3>No documents found</h3>
                  <p>Upload your first document to get started</p>
                  <button onClick={handleShowUpload}>Upload Document</button>
                </div>
              ) : (
                <DocumentsGrid documents={documents} onDelete={handleDelete} onSelect={handleSelect} />
              )}
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
      </main>
    </div>
  )
}

export default App
