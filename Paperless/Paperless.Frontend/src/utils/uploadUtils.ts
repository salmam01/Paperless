//uploas consts
export const ACCEPTED_FILE_TYPES = ['.pdf', '.doc', '.docx', '.txt', '.jpg', '.jpeg', '.png'];
export const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB

// Upload-Utility-Function
export function validateFile(file: File): string | null {
    const fileExtension = '.' + file.name.split('.').pop()?.toLowerCase();
    
    if (!ACCEPTED_FILE_TYPES.includes(fileExtension)) {
        return `Dateityp ${fileExtension} wird nicht unterstützt. Erlaubte Formate: ${ACCEPTED_FILE_TYPES.join(', ')}`;
    }
    
    if (file.size > MAX_FILE_SIZE) {
        return `Datei ist zu groß. Maximum: ${Math.round(MAX_FILE_SIZE / 1024 / 1024)}MB`;
    }
    
    return null;
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
