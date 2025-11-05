// Upload-consts
export const ACCEPTED_FILE_TYPES = ['.pdf', '.doc', '.docx', '.txt'];
export const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB

// Upload-Utility-Function
export function validateFile(file: File): string | null {
    const fileExtension = '.' + file.name.split('.').pop()?.toLowerCase();
    
    // Debug-Logging : txt files
    if (fileExtension === '.txt') {
        console.log('TXT-Datei erkannt:', {
            fileName: file.name,
            fileExtension,
            fileSize: file.size,
            contentType: file.type,
            acceptedTypes: ACCEPTED_FILE_TYPES
        });
    }
    
    if (!ACCEPTED_FILE_TYPES.includes(fileExtension)) {
        return `Dateityp ${fileExtension} wird nicht unterstützt. Erlaubte Formate: ${ACCEPTED_FILE_TYPES.join(', ')}`;
    }
    
    if (file.size > MAX_FILE_SIZE) {
        return `Datei ist zu groß. Maximum: ${Math.round(MAX_FILE_SIZE / 1024 / 1024)}MB`;
    }
    
    return null;
}

// filetype
export function getFileTypeFromFileName(fileName: string): string {
    if (!fileName) return 'Unknown';
    const extension = fileName.split('.').pop()?.toLowerCase();
    
    switch (extension) {
        case 'pdf': return 'PDF';
        case 'doc': return 'DOC';
        case 'docx': return 'DOCX';
        case 'txt': return 'TXT';
        default: return 'Unknown';
    }
}

export function formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

export function clearFileInput(fileInputRef: React.RefObject<HTMLInputElement | null>) {
    if (fileInputRef.current) {
        fileInputRef.current.value = '';
    }
}
