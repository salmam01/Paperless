import { useState, useEffect } from 'react';
import type { DocumentDto } from "../dto/DocumentDto";
import { getCategory, getCategories } from '../services/CategoryService';
import { putDocumentCategory } from '../services/DocumentService';
import type { CategoryDto } from '../dto/CategoryDto';

interface Props {
    document: DocumentDto;
    onBack?: () => void;
    onCategoryUpdate?: () => void;
}

export function DocumentDetails({ document, onBack, onCategoryUpdate }: Props) {
    const [category, setCategory] = useState<CategoryDto | null>(null);
    const [categories, setCategories] = useState<CategoryDto[]>([]);
    const [isEditingCategory, setIsEditingCategory] = useState(false);
    const [selectedCategoryId, setSelectedCategoryId] = useState<string | null>(document.categoryId || null);
    const [isUpdating, setIsUpdating] = useState(false);

    useEffect(() => {
        if (document.categoryId) {
            getCategory(document.categoryId)
                .then(setCategory)
                .catch(() => setCategory(null));
        } else {
            setCategory(null);
        }
        setSelectedCategoryId(document.categoryId || null);
    }, [document.categoryId]);

    useEffect(() => {
        getCategories()
            .then(setCategories)
            .catch(() => setCategories([]));
    }, []);

    const handleCategoryChange = async (newCategoryId: string | null) => {
        try {
            setIsUpdating(true);
            if (newCategoryId) {
                await putDocumentCategory(document.id, newCategoryId);
            } else {
                // Wenn null, könnte man eine spezielle API-Route brauchen, oder einfach die erste Kategorie setzen
                // For jz: nur wenn eine Kategorie ausgewählt ist
                return;
            }
            
            const updatedCategory = await getCategory(newCategoryId);
            setCategory(updatedCategory);
            setSelectedCategoryId(newCategoryId);
            setIsEditingCategory(false);
            onCategoryUpdate?.(); // Callback zum refresh d. Dokumente
        } catch (err) {
            console.error('Failed to update category:', err);
        } finally {
            setIsUpdating(false);
        }
    };

    return (
        <div className="document-details">
            <div className="document-details-header">
                <h2 className="document-details-title">{document.name}</h2>
                <button className="document-details-close" onClick={onBack} aria-label="Close">×</button>
            </div>
            
            {isEditingCategory ? (
                <div className="document-category-edit">
                    <select
                        value={selectedCategoryId || ''}
                        onChange={(e) => setSelectedCategoryId(e.target.value || null)}
                        className="category-select-edit"
                        disabled={isUpdating}
                    >
                        <option value="">No Category</option>
                        {categories.map(cat => (
                            <option key={cat.id} value={cat.id}>{cat.name}</option>
                        ))}
                    </select>
                    <button
                        className="category-save-btn"
                        onClick={() => handleCategoryChange(selectedCategoryId)}
                        disabled={isUpdating || selectedCategoryId === document.categoryId}
                        title="Save"
                    >
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                            <polyline points="20 6 9 17 4 12" />
                        </svg>
                    </button>
                    <button
                        className="category-cancel-btn"
                        onClick={() => {
                            setIsEditingCategory(false);
                            setSelectedCategoryId(document.categoryId || null);
                        }}
                        disabled={isUpdating}
                        title="Cancel"
                    >
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                            <line x1="18" y1="6" x2="6" y2="18" />
                            <line x1="6" y1="6" x2="18" y2="18" />
                        </svg>
                    </button>
                </div>
            ) : (
                <div className="document-category-badge">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                        <path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z"/>
                    </svg>
                    <span>{category ? category.name : 'No Category'}</span>
                    <button
                        className="category-edit-icon"
                        onClick={() => setIsEditingCategory(true)}
                        title="Edit category"
                    >
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
                        </svg>
                    </button>
                </div>
            )}
            
            <div className="tabs">
                <input type="radio" id="tab-summary" name="details-tabs" defaultChecked />
                <label htmlFor="tab-summary">Summary</label>
                <input type="radio" id="tab-content" name="details-tabs" />
                <label htmlFor="tab-content">Content</label>
                <input type="radio" id="tab-meta" name="details-tabs" />
                <label htmlFor="tab-meta">Meta</label>

                <div className="tab-panels">
                    <section className="tab-panel" data-for="tab-summary">
                        <p>{document.summary || 'No summary available yet. The summary is being generated...'}</p>
                    </section>
                    <section className="tab-panel" data-for="tab-content">
                        <p>{document.content || 'No content available.'}</p>
                    </section>
                    <section className="tab-panel" data-for="tab-meta">
                        <div className="meta-item">
                            <strong>Type:</strong> {document.type || 'Unknown'}
                        </div>
                        <div className="meta-item">
                            <strong>Size:</strong> {Math.round(document.size * 100) / 100} MB
                        </div>
                        <div className="meta-item">
                            <strong>Created:</strong> {new Date(document.creationDate).toLocaleDateString('de-DE', { 
                                day: '2-digit', 
                                month: '2-digit', 
                                year: 'numeric',
                                hour: '2-digit',
                                minute: '2-digit'
                            })}
                        </div>
                        {category && (
                            <div className="meta-item">
                                <strong>Category:</strong> {category.name}
                            </div>
                        )}
                    </section>
                </div>
            </div>
        </div>
    );
}
