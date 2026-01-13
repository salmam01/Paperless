import {useEffect, useState} from 'react';
import type {DocumentDto} from "../dto/DocumentDto";
import {getCategory} from '../services/CategoryService';
import type {CategoryDto} from '../dto/CategoryDto';

interface Props {
    document: DocumentDto;
    onDelete?: (id: string) => void;
    onSelect?: (id: string) => void;
}

//  Zuweisung von Farben basierend auf Kategorie-ID
const getCategoryColor = (categoryId: string): string => {
    let hash = 0;
    for (let i = 0; i < categoryId.length; i++) {
        hash = categoryId.charCodeAt(i) + ((hash << 5) - hash);
    }

    // Farbpaletten 
    const colors = [
        'rgba(102, 126, 234, 0.4)',   // Blau-Lila
        'rgba(118, 75, 162, 0.4)',    // Lila
        'rgba(139, 92, 246, 0.4)',    // Violett
        'rgba(168, 85, 247, 0.4)',    // Lila-Violett
        'rgba(192, 132, 252, 0.4)',   // Hell-Lila
        'rgba(147, 51, 234, 0.4)',    // Dunkel-Violett
        'rgba(124, 58, 237, 0.4)',    // Indigo
        'rgba(99, 102, 241, 0.4)',    // Indigo-Blau
    ];

    return colors[Math.abs(hash) % colors.length];
};

export function DocumentCard({document, onDelete, onSelect}: Props) {
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
        <div className="document-card">
            {category && (
                <div
                    className="document-category-badge-top"
                    style={{
                        background: getCategoryColor(category.id),
                        borderColor: getCategoryColor(category.id).replace('0.4', '0.6')
                    }}
                >
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
                         strokeLinecap="round" strokeLinejoin="round">
                        <path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z"/>
                    </svg>
                    <span>{category.name}</span>
                </div>
            )}
            <div className="document-header">
                <h3>{document.name}</h3>
                <span className="document-type">{document.type}</span>
            </div>
            <div className="document-content-wrapper">
                <p className="document-content">
                    {document.summary}
                </p>
            </div>
            <div className="document-metadata">
                <div className="document-metadata-left">
                    {onSelect && (
                        <button
                            className="card-action-button card-action-button--info"
                            aria-label="Open details"
                            title="Details"
                            onClick={(e) => {
                                e.stopPropagation();
                                onSelect(document.id);
                            }}
                        >
                            <svg
                                xmlns="http://www.w3.org/2000/svg"
                                width="20"
                                height="20"
                                viewBox="0 0 24 24"
                                fill="none"
                                stroke="currentColor"
                                strokeWidth="2"
                                strokeLinecap="round"
                                strokeLinejoin="round"
                            >
                                <circle cx="12" cy="12" r="10"/>
                                <line x1="12" y1="16" x2="12" y2="12"/>
                                <line x1="12" y1="8" x2="12.01" y2="8"/>
                            </svg>
                        </button>
                    )}
                    <span className="document-size">{Math.round(document.size * 100) / 100} MB</span>
                </div>
                <div className="document-metadata-right">
                    <span className="document-date">{new Date(document.creationDate).toLocaleDateString('de-DE')}</span>
                    {onDelete && (
                        <button
                            className="card-action-button card-action-button--delete"
                            aria-label="Delete document"
                            title="Delete"
                            onClick={(e) => {
                                e.stopPropagation();
                                onDelete(document.id);
                            }}
                        >
                            <svg
                                xmlns="http://www.w3.org/2000/svg"
                                width="20"
                                height="20"
                                viewBox="0 0 24 24"
                                fill="none"
                                stroke="currentColor"
                                strokeWidth="2"
                                strokeLinecap="round"
                                strokeLinejoin="round"
                            >
                                <polyline points="3 6 5 6 21 6"/>
                                <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
                                <path d="M10 11v6"/>
                                <path d="M14 11v6"/>
                                <path d="M9 6V4a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v2"/>
                            </svg>
                        </button>
                    )}
                </div>
            </div>
        </div>
    );
}