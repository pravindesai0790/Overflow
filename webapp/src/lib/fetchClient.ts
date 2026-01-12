import {notFound} from "next/dist/client/components/not-found";

export async function fetchClient<T>(
    url: string, 
    method: 'GET' | 'POST' | 'PUT' | 'DELETE', 
    options: Omit<RequestInit, 'body'> & {body?: unknown} = {}
): Promise<{data: T | null, error?: {message: string, status: number}}> {
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

    const contentType = response.headers.get('Content-type');
    const isJson = contentType?.includes('application/json')
        || contentType?.includes('application/problem+json');
    const parsedData = isJson ? await response.json() : await response.text();
    
    if(!response.ok) {
        if(response.status === 404) return notFound();
        if(response.status === 500) throw new Error('Server error, Please, try again later.');
        
        let message = '';
        
        if(typeof parsedData === 'string') {
            message = parsedData;
        } else if(parsedData?.message) {
            message = parsedData?.message;
        }
        
        if(!message) {
            message = getFallbackMessage(response.status);
        }
        
        return {data: null, error: {message, status: response.status}}
    }
    
    return {data: parsedData as T};
}

function getFallbackMessage(status: number) {
    switch (status) {
        case 400: return 'Bad Request. Please check your input';
        case 401: return 'You must be logged in';
        case 403: return 'You do not have access to this page.';
        case 500: return 'Server Error. Please try again later.';
        default: return 'An Unexpected error occurred. Please try again later.';
    }
}