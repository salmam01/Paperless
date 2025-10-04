import type { DocumentDto } from "../dto/documentDto";

interface Props {
    document: DocumentDto;
    onBack?: () => void;
}

export function DocumentDetails({ document, onBack }: Props) {
    return (
        <div className="document-details">
            <div className="document-details-header">
                <h2 className="document-details-title">{document.name}</h2>
                <button className="document-details-close" onClick={onBack} aria-label="Close">×</button>
            </div>
            <div className="tabs">
                <input type="radio" id="tab-summary" name="details-tabs" defaultChecked />
                <label htmlFor="tab-summary">Summary</label>
                <input type="radio" id="tab-content" name="details-tabs" />
                <label htmlFor="tab-content">Content</label>
                <input type="radio" id="tab-meta" name="details-tabs" />
                <label htmlFor="tab-meta">Meta</label>

                <div className="tab-panels">
                    <section className="tab-panel" data-for="tab-summary">
                        <p>{document.summary}</p>
                    </section>
                    <section className="tab-panel" data-for="tab-content">
                        <p>{document.content}</p>
                    </section>
                    <section className="tab-panel" data-for="tab-meta">
                        <p><strong>Type:</strong> {document.type}</p>
                        <p><strong>Size:</strong> {document.size} bytes</p>
                        <p><strong>Created:</strong> {new Date(document.creationDate).toLocaleString()}</p>
                    </section>
                </div>
            </div>
        </div>
    );
}


