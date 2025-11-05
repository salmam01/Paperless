import { useState, useRef, type ChangeEvent, type DragEvent } from "react";
import { validateFile, formatFileSize, clearFileInput, ACCEPTED_FILE_TYPES } from "../utils/uploadUtils";

interface Props {
    loading: boolean;
    onUploaded: (file: File) => void;
    onBack?: () => void;
}

export function UploadPanel({ loading, onUploaded, onBack }: Props) {
    const [file, setFile] = useState<File | null>(null);
    const [isDragOver, setIsDragOver] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const fileInputRef = useRef<HTMLInputElement | null>(null);

    function handleFileSelect(selectedFile: File) {
        const validationError = validateFile(selectedFile);
        if (validationError) {
            setError(validationError);
            return;
        }
        
        setError(null);
        setFile(selectedFile);
    }

    function handleFileChange(e: ChangeEvent<HTMLInputElement>) {
        if (e.target.files && e.target.files.length > 0) {
            handleFileSelect(e.target.files[0]);
        }
    }

    function handleDragOver(e: DragEvent<HTMLDivElement>) {
        e.preventDefault();
        setIsDragOver(true);
    }

    function handleDragLeave(e: DragEvent<HTMLDivElement>) {
        e.preventDefault();
        setIsDragOver(false);
    }

    function handleDrop(e: DragEvent<HTMLDivElement>) {
        e.preventDefault();
        setIsDragOver(false);
        
        const droppedFiles = e.dataTransfer.files;
        if (droppedFiles.length > 0) {
            handleFileSelect(droppedFiles[0]);
        }
    }

    function handleUpload() {
        if (file) {
            onUploaded(file);
            setFile(null);
            setError(null);
            clearFileInput(fileInputRef);
        }
    }

    function handleRemoveFile() {
        setFile(null);
        setError(null);
        clearFileInput(fileInputRef);
    }

    return (
        <div className="upload-panel">
            <div className="document-details-header">
                <h2 className="document-details-title">Upload Document</h2>
                <button className="document-details-close" onClick={onBack} aria-label="Close">×</button>
            </div>
            
            <div className="upload-panel-content">
                <div 
                    className={`upload-dropzone ${isDragOver ? 'drag-over' : ''} ${file ? 'has-file' : ''}`}
                    onDragOver={handleDragOver}
                    onDragLeave={handleDragLeave}
                    onDrop={handleDrop}
                    onClick={() => fileInputRef.current?.click()}
                >
                    <input 
                        ref={fileInputRef}
                        type="file" 
                        name="document" 
                        onChange={handleFileChange}
                        accept={ACCEPTED_FILE_TYPES.join(',')}
                        style={{ display: 'none' }}
                    />
                    
                    {!file ? (
                        <div className="upload-content">
                            <div className="upload-icon">
                                <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                                    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                                    <polyline points="17 8 12 3 7 8"/>
                                    <line x1="12" y1="3" x2="12" y2="15"/>
                                </svg>
                            </div>
                            <h3>Drag & Drop</h3>
                            <p>or click to choose</p>
                            <div className="upload-formats">
                                <small>Supported file formats: PDF, DOCX, TXT</small>
                                <small style={{ display: 'block', marginTop: '8px' }}>Maximum File Size: 1 GB</small>
                            </div>
                        </div>
                    ) : (
                        <div className="file-preview">
                            <div className="file-icon">
                                <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                                    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                                    <polyline points="14,2 14,8 20,8"/>
                                </svg>
                            </div>
                            <div className="file-info">
                                <h4>{file.name}</h4>
                                <p>{formatFileSize(file.size)}</p>
                            </div>
                            <button 
                                className="remove-file-btn"
                                onClick={(e) => {
                                    e.stopPropagation();
                                    handleRemoveFile();
                                }}
                            >
                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                                    <line x1="18" y1="6" x2="6" y2="18"/>
                                    <line x1="6" y1="6" x2="18" y2="18"/>
                                </svg>
                            </button>
                        </div>
                    )}
                </div>
                
                {error && (
                    <div className="upload-error">
                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                            <circle cx="12" cy="12" r="10"/>
                            <line x1="15" y1="9" x2="9" y2="15"/>
                            <line x1="9" y1="9" x2="15" y2="15"/>
                        </svg>
                        {error}
                    </div>
                )}
                
                <div className="upload-actions-panel">
                    <button 
                        className="upload-btn-panel"
                        onClick={handleUpload}
                        disabled={!file || loading}
                    >
                        {loading ? (
                            <>
                                <svg className="spinner" width="18" height="18" viewBox="0 0 24 24">
                                    <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="2" fill="none" strokeDasharray="31.416" strokeDashoffset="31.416">
                                        <animate attributeName="stroke-dasharray" dur="2s" values="0 31.416;15.708 15.708;0 31.416" repeatCount="indefinite"/>
                                        <animate attributeName="stroke-dashoffset" dur="2s" values="0;-15.708;-31.416" repeatCount="indefinite"/>
                                    </circle>
                                </svg>
                                Uploading...
                            </>
                        ) : (
                            <>
                                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                                    <polyline points="20 6 9 17 4 12"/>
                                </svg>
                                Upload
                            </>
                        )}
                    </button>
                </div>
            </div>
        </div>
    );
}


