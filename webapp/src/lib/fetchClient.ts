export async function fetchClient<T>(
    url: string, 
    method: 'GET' | 'POST' | 'PUT' | 'DELETE', 
    options: Omit<RequestInit, 'body'> & {body?: unknown} = {}): Promise<T> {
    const { body, ...rest } = options;
    const apiUrl = process.env.API_URL;
    if (!apiUrl) throw new Error('Missing API URL');
    
    const headers: HeadersInit = {
        'Content-Type': 'application/json',
        ...(rest.headers || {})
    }
    
    const response = await fetch(apiUrl + url, {
        method,
        headers,
        ...(body ? {body: JSON.stringify(body)} : {}),
        ...rest
    })
    
    if(!response.ok) {
        const contentType = response.headers.get('Content-type');
        const isJson = contentType?.includes('application/json') 
            || contentType?.includes('application/problem+json');
        const errorData = isJson ? await response.json() : await response.text();
        
        throw new Error(`${errorData || 'An error occurred while fetching client response'}`);
    }
    
    return response.json();
}