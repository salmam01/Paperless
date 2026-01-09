import type { CategoryDto } from "../dto/CategoryDto";

const apiUrl = 'http://localhost:8080/api/category'

export async function getCategories(): Promise<CategoryDto[]> {
    const response = await fetch (apiUrl);

    if (!response.ok) 
        throw new Error ('Failed to fetch categories.');

    return await response.json();
}

export async function getCategory(id: string): Promise<CategoryDto>{
    const response = await fetch(`${apiUrl}/${id}`);

    if (!response.ok) 
        throw new Error(`Failed to fetch category with ID: ${id}`);

    return await response.json();
}

export async function createCategory(name: string): Promise<CategoryDto> {
    const postUrl = `${apiUrl}`
    
    const response = await fetch(postUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(name)
    });

    if (!response.ok) 
        throw new Error (`Failed to create category.`);
    
    return await response.json();
}

export async function deleteCategory(id: string): Promise<void> {
    const response = await fetch(`${apiUrl}/${id}`, {
        method: 'DELETE'
    });

    if (!response.ok) 
        throw new Error(`Failed to delete category with ID: ${id}`)
}