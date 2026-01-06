import { useState, useEffect } from 'react';
import type { DocumentDto } from "../dto/documentDto";
import { getCategory } from '../services/categoryService';
import type { CategoryDto } from '../dto/categoryDto';

interface Props {
    document: DocumentDto;
    onBack?: () => void;
}

export function DocumentDetails({ document, onBack }: Props) {
    const [category, setCategory] = useState<CategoryDto | null>(null);

    useEffect(() => {
        if (document.categoryId) {
            getCategory(document.categoryId)
                .then(setCategory)
                .catch(() => setCategory(null));
        } else {
            setCategory(null);
        }
    }, [document.categoryId]);

    return (
        <div className="document-details">
            <div className="document-details-header">
                <h2 className="document-details-title">{document.name}</h2>
                <button className="document-details-close" onClick={onBack} aria-label="Close">×</button>
            </div>
            
            {category && (
                <div className="document-category-badge">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                        <path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z"/>
                    </svg>
                    <span>{category.name}</span>
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
                            <strong>Created:</strong> {new Date(document.creationDate).toLocaleString()}
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
