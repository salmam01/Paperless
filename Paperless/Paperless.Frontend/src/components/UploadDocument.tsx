import { useState, type ChangeEvent } from "react";

interface Props {
    loading: boolean
    onUploaded: (file: File) => void;
}

export function UploadDocument( { loading, onUploaded } : Props) {
    const [file, setFile] = useState<File | null>(null);

    function handleFileChange(e: ChangeEvent<HTMLInputElement>) {
        if (e.target.files && e.target.files.length > 0) {
            setFile(e.target.files[0]);
        }
    }

    return(
        <div>
            <input type="file" name="document" onChange={handleFileChange}/>
            <button 
                onClick={() => {
                    if (file) onUploaded(file)
                }}
                disabled={!file || loading}
            >
                {loading ? "Uploading..." : "Upload"}
            </button>
        </div>
    );
}