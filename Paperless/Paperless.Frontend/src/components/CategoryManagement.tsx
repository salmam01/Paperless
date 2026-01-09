import { useState, useEffect } from 'react';
import { getCategories, createCategory, deleteCategory } from '../services/CategoryService';
import type { CategoryDto } from '../dto/CategoryDto';

interface Props {
    onCategorySelect?: (categoryId: string | null) => void;
    selectedCategoryId?: string | null;
    onCategoryDelete?: (categoryId: string) => void;
    showCreateForm?: boolean;
    compact?: boolean;
}

export function CategoryManagement({ onCategorySelect, selectedCategoryId, onCategoryDelete, showCreateForm = true, compact = false }: Props) {
    const [categories, setCategories] = useState<CategoryDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [newCategoryName, setNewCategoryName] = useState('');
    const [isCreating, setIsCreating] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        fetchCategories();
    }, []);

    const fetchCategories = async () => {
        try {
            setLoading(true);
            setError(null);
            const data = await getCategories();
            setCategories(data);
        } catch (err) {
            const errorMessage = err instanceof Error ? err.message : 'Failed to load categories';
            setError(errorMessage);
        } finally {
            setLoading(false);
        }
    };

    const handleCreateCategory = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!newCategoryName.trim()) return;

        try {
            setIsCreating(true);
            setError(null);
            const newCategory = await createCategory(newCategoryName.trim());
            setCategories(prev => [...prev, newCategory]);
            setNewCategoryName('');
        } catch (err) {
            const errorMessage = err instanceof Error ? err.message : 'Failed to create category';
            setError(errorMessage);
        } finally {
            setIsCreating(false);
        }
    };

const handleDeleteCategory = async (id: string) => {
    try {
        await deleteCategory(id);
        setCategories(prev => prev.filter(cat => cat.id !== id));
        onCategoryDelete?.(id);
    } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Failed to delete category';
        setError(errorMessage);
    }
};

    if (compact) {
        return (
            <div className="category-management-compact">
                <label className="category-label">Category</label>
                {loading ? (
                    <div className="category-loading-compact">Loading...</div>
                ) : error ? (
                    <div className="category-error-compact">{error}</div>
                ) : (
                    <div className="category-select-compact">
                        {onCategorySelect && (
                            <button
                                type="button"
                                className={`category-option ${selectedCategoryId === null ? 'selected' : ''}`}
                                onClick={() => onCategorySelect(null)}
                            >
                                <span>No Category</span>
                            </button>
                        )}
                        {categories.map(category => (
                            <button
                                key={category.id}
                                type="button"
                                className={`category-option ${selectedCategoryId === category.id ? 'selected' : ''}`}
                                onClick={() => onCategorySelect?.(category.id)}
                            >
                                <span>{category.name}</span>
                            </button>
                        ))}
                    </div>
                )}
                {showCreateForm && (
                    <div className="category-create-compact">
                        <input
                            type="text"
                            value={newCategoryName}
                            onChange={(e) => setNewCategoryName(e.target.value)}
                            placeholder="Create new category..."
                            disabled={isCreating}
                            className="category-input-compact"
                            onKeyDown={(e) => {
                                if (e.key === 'Enter' && newCategoryName.trim()) {
                                    handleCreateCategory(e);
                                }
                            }}
                        />
                        <button
                            type="button"
                            onClick={handleCreateCategory}
                            disabled={isCreating || !newCategoryName.trim()}
                            className="category-create-btn-compact"
                            title="Create category"
                        >
                            {isCreating ? (
                                <svg className="spinner" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                                    <circle cx="12" cy="12" r="10" strokeDasharray="31.416" strokeDashoffset="31.416">
                                        <animate attributeName="stroke-dasharray" dur="2s" values="0 31.416;15.708 15.708;0 31.416" repeatCount="indefinite"/>
                                        <animate attributeName="stroke-dashoffset" dur="2s" values="0;-15.708;-31.416" repeatCount="indefinite"/>
                                    </circle>
                                </svg>
                            ) : (
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                    <line x1="12" y1="5" x2="12" y2="19"/>
                                    <line x1="5" y1="12" x2="19" y2="12"/>
                                </svg>
                            )}
                        </button>
                    </div>
                )}
                {error && <div className="category-error-compact">{error}</div>}
            </div>
        );
    }

    return (
        <div className="category-management">
            {showCreateForm && (
                <div className="category-create-form">
                    <h3>Create Category</h3>
                    <form onSubmit={handleCreateCategory}>
                        <div className="category-input-group">
                            <input
                                type="text"
                                value={newCategoryName}
                                onChange={(e) => setNewCategoryName(e.target.value)}
                                placeholder="Enter category name..."
                                disabled={isCreating}
                                className="category-input"
                            />
                            <button type="submit" disabled={isCreating || !newCategoryName.trim()} className="category-create-btn">
                                {isCreating ? (
                                    <>
                                        <svg className="spinner" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                                            <circle cx="12" cy="12" r="10" strokeDasharray="31.416" strokeDashoffset="31.416">
                                                <animate attributeName="stroke-dasharray" dur="2s" values="0 31.416;15.708 15.708;0 31.416" repeatCount="indefinite"/>
                                                <animate attributeName="stroke-dashoffset" dur="2s" values="0;-15.708;-31.416" repeatCount="indefinite"/>
                                            </circle>
                                        </svg>
                                        Creating...
                                    </>
                                ) : (
                                    <>
                                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                            <line x1="12" y1="5" x2="12" y2="19"/>
                                            <line x1="5" y1="12" x2="19" y2="12"/>
                                        </svg>
                                        Create
                                    </>
                                )}
                            </button>
                        </div>
                        {error && <div className="category-error">{error}</div>}
                    </form>
                </div>
            )}

            <div className="category-list-section">
                <div className="category-list-header">
                    <h3>Categories</h3>
                    <span className="category-count">{categories.length}</span>
                </div>
                {loading ? (
                    <div className="category-loading">
                        <svg className="spinner" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                            <circle cx="12" cy="12" r="10" strokeDasharray="31.416" strokeDashoffset="31.416">
                                <animate attributeName="stroke-dasharray" dur="2s" values="0 31.416;15.708 15.708;0 31.416" repeatCount="indefinite"/>
                                <animate attributeName="stroke-dashoffset" dur="2s" values="0;-15.708;-31.416" repeatCount="indefinite"/>
                            </circle>
                        </svg>
                        Loading categories...
                    </div>
                ) : error ? (
                    <div className="category-error">{error}</div>
                ) : categories.length === 0 ? (
                    <div className="category-empty">
                        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                            <path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z"/>
                            <line x1="7" y1="7" x2="7.01" y2="7"/>
                        </svg>
                        <p>No categories yet</p>
                        <span>Create one to get started!</span>
                    </div>
                ) : (
                    <div className="category-list">
                        {onCategorySelect && (
                            <button
                                type="button"
                                className={`category-item ${selectedCategoryId === null ? 'selected' : ''}`}
                                onClick={() => onCategorySelect(null)}
                            >
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                    <circle cx="12" cy="12" r="10"/>
                                    <line x1="12" y1="8" x2="12" y2="16"/>
                                    <line x1="8" y1="12" x2="16" y2="12"/>
                                </svg>
                                <span>No Category</span>
                            </button>
                        )}
                        {categories.map(category => (
                            <div key={category.id} className="category-item-wrapper">
                                <button
                                    type="button"
                                    className={`category-item ${selectedCategoryId === category.id ? 'selected' : ''}`}
                                    onClick={() => onCategorySelect?.(category.id)}
                                >
                                    <span>{category.name}</span>
                                </button>

                                <button
                                    className="card-action-button card-action-button--delete"
                                    aria-label="Delete category"
                                    title="Delete"
                                    onClick={(e) => { e.stopPropagation(); handleDeleteCategory(category.id); }}
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
                                        <polyline points="3 6 5 6 21 6" />
                                        <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                                        <path d="M10 11v6" />
                                        <path d="M14 11v6" />
                                        <path d="M9 6V4a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v2" />
                                    </svg>
                                </button>
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
}










