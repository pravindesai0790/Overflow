import {notFound} from "next/dist/client/components/not-found";
import {auth} from "@/auth";
import {apiConfig} from "@/lib/config";

export async function fetchClient<T>(
    url: string, 
    method: 'GET' | 'POST' | 'PUT' | 'DELETE', 
    options: Omit<RequestInit, 'body'> & {body?: unknown} = {}
): Promise<{data: T | null, error?: {message: string, status: number}}> {
    const { body, ...rest } = options;
    const apiUrl = apiConfig.baseUrl;
    if (!apiUrl) throw new Error('Missing API URL');
    const session = await auth();
    
    const headers: HeadersInit = {
        'Content-Type': 'application/json',
        ...(session?.accessToken ? {Authorization: `Bearer ${session.accessToken}`} : {}),
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
        
        if(response.status === 401) {
            const authHeader = response.headers.get('WWW-Authenticate');
            if(authHeader?.includes('error_description')) {
                const match = authHeader?.match(/error_description="(.+?)"/);
                if(match) message = match[1];
            } else {
                message = 'You must be logged in to do that';
            }
        }
        
        if(!message) {
            if(typeof parsedData === 'string') {
                message = parsedData;
            } else if(parsedData?.message) {
                message = parsedData?.message;
            } else {
                message = getFallbackMessage(response.status);
            }
        }
        
        return {data: null, error: {message, status: response.status}}
    }
    
    return {data: parsedData as T};
}

function getFallbackMessage(status: number) {
    switch (status) {
        case 400: return 'Bad Request. Please check your input';
        case 403: return 'You do not have access to this page.';
        case 500: return 'Server Error. Please try again later.';
        default: return 'An Unexpected error occurred. Please try again later.';
    }
}