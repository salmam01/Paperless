import {FaSearch} from "react-icons/fa"

interface Props {
    query: string;
    onChange: (value:string) => void;
}

export function Searchbar({ query, onChange }: Props) {

    return(
        <div className="searchbar-section">
            <FaSearch id="search-icon" />
            <input 
                placeholder="Type to search..." 
                value={query}
                onChange={(e) => onChange(e.target.value)}
            />
        </div>
    )
}