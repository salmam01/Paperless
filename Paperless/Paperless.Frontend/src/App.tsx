import { useState, useEffect } from 'react'
import './App.css'
import type { DocumentDto } from './dto/DocumentDto'
import type { CategoryDto } from './dto/CategoryDto'
import { getDocuments, getDocument, getSearchResult, deleteDocument, deleteDocuments, postDocument } from './services/DocumentService'
import { getCategories } from './services/CategoryService'
import { DocumentsGrid } from './components/DocumentsGrid'
import { DocumentDetails } from './components/DocumentDetails'
import { Searchbar } from './components/Searchbar'
import { UploadPanel } from './components/UploadPanel'
import { CategoryManagement } from './components/CategoryManagement'

function App() {
  const [documents, setDocuments] = useState<DocumentDto[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [selectedDocument, setSelectedDocument] = useState<DocumentDto | null>(null);
  const [loading, setLoading] = useState(true);
  //const [showSearchbar, setShowSearchbar] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");
  const [showUploadPanel, setShowUploadPanel] = useState(false);
  const [showCategoryManagement, setShowCategoryManagement] = useState(false);
  const [notifications, setNotifications] = useState<Array<{ id: string, message: string, type: 'error' | 'success' | 'info' }>>([]);
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [selectedCategoryFilter, setSelectedCategoryFilter] = useState<string | null>(null);

  useEffect(() => {
    fetchDocuments();
    fetchCategories();
  }, []);

  const fetchCategories = async () => {
    try {
      const data = await getCategories();
      setCategories(data);
    } catch (err) {
      console.error('Failed to load categories:', err);
    }
  };

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
    setShowCategoryManagement(false);
  };

  const handleShowUpload = () => {
    setSelectedId(null);
    setSelectedDocument(null);
    setShowUploadPanel(true);
    setShowCategoryManagement(false);
  };

  const handleShowCategoryManagement = () => {
    setSelectedId(null);
    setSelectedDocument(null);
    setShowUploadPanel(false);
    setShowCategoryManagement(true);
  };

  //  for button, later
  /*const handleShowSearchbar = () => {
    setShowSearchbar(true);
  }*/

  const handleSearch = async (query: string) => {
    const trimmedQuery = query.trim();

    if (trimmedQuery === "") {
      await fetchDocuments()
    } else {
      setLoading(true);
      const result = await getSearchResult(trimmedQuery);
      setDocuments(result);
      setLoading(false);
    }
  }

  const handleCategoryFilter = async (categoryId: string | null) => {
    setSelectedCategoryFilter(categoryId);
    if (categoryId) {
      // Filter existing documents
      const allDocs = await getDocuments();
      const filtered = allDocs.filter(doc => doc.categoryId === categoryId);
      setDocuments(filtered);
    } else {
      await fetchDocuments();
    }
  }

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
  const handleUpload = async (file: File, categoryId?: string | null) => {
    try {
      if (!file) return;

      // Debug-Logging: für TXT-Dateien
      if (file.name.toLowerCase().endsWith('.txt')) {
        console.log('TXT-Upload gestartet:', {
          fileName: file.name,
          fileSize: file.size,
          fileType: file.type,
          lastModified: file.lastModified,
          categoryId: categoryId
        });
      }

      setLoading(true);
      const formData = new FormData();
      formData.append("form", file);

      // Add category IDs if provided
      if (categoryId) {
        formData.append("categoryIds", categoryId);
      }

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
          <Searchbar
            query={searchQuery}
            onChange={(val) => {
              setSearchQuery(val);
              handleSearch(val);
            }} />
          <div className="documents-toolbar">
            <button onClick={handleShowCategoryManagement} disabled={loading}>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M4 7h16M4 12h16M4 17h16" />
              </svg>
              Categories
            </button>
            <div className="category-filter-toolbar">
              <button
                className={`category-filter-btn ${selectedCategoryFilter === null ? 'active' : ''}`}
                onClick={() => handleCategoryFilter(null)}
                disabled={loading}
                title="All Categories"
              >
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <circle cx="12" cy="12" r="10" />
                  <line x1="12" y1="8" x2="12" y2="16" />
                  <line x1="8" y1="12" x2="16" y2="12" />
                </svg>
                All
              </button>
              {categories.map(category => (
                <button
                  key={category.id}
                  className={`category-filter-btn ${selectedCategoryFilter === category.id ? 'active' : ''}`}
                  onClick={() => handleCategoryFilter(category.id)}
                  disabled={loading}
                  title={`Filter by ${category.name}`}
                >
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z" />
                  </svg>
                  {category.name}
                </button>
              ))}
            </div>
            <button onClick={handleShowUpload} disabled={loading}>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                <polyline points="17 8 12 3 7 8" />
                <line x1="12" y1="3" x2="12" y2="15" />
              </svg>
              Upload
            </button>
            <button onClick={handleDeleteAll} disabled={loading || documents.length === 0}>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <polyline points="3 6 5 6 21 6" />
                <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                <path d="M10 11v6" />
                <path d="M14 11v6" />
                <path d="M9 6V4a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v2" />
              </svg>
              Delete All
            </button>
            <button onClick={fetchDocuments} disabled={loading}>
              {loading ? (
                <>
                  <svg className="spinner" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M21.5 2v6h-6M2.5 22v-6h6M2 11.5a10 10 0 0 1 18.8-4.3M22 12.5a10 10 0 0 1-18.8 4.2" />
                  </svg>
                  Refreshing...
                </>
              ) : (
                <>
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <polyline points="23 4 23 10 17 10" />
                    <polyline points="1 20 1 14 7 14" />
                    <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15" />
                  </svg>
                  Refresh
                </>
              )}
            </button>
          </div>
          {showUploadPanel && (
            <div className="upload-section">
              <UploadPanel loading={loading} onUploaded={handleUpload} onBack={handleBack} />
            </div>
          )}
          {showCategoryManagement && (
            <div className="category-section">
              <div className="category-panel">
                <div className="category-panel-header">
                  <h2>Category Management</h2>
                  <button className="category-panel-close" onClick={handleBack} aria-label="Close">×</button>
                </div>
                <CategoryManagement showCreateForm={true} />
              </div>
            </div>
          )}
          <div className={`documents-layout ${selectedDocument ? 'with-panel' : ''}`}>
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
          </div>
        </div>
      </main>
    </div>
  )
}

export default App
