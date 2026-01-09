'use server'

import {Question} from "@/lib/types";

export async function getQuestions(tags?: string): Promise<Question[]> {
    let url = 'http://localhost:8001/questions';
    if (tags) url += `?tags=${tags}`;
    const response = await fetch(url);
    
    if(!response.ok) throw new Error('Failed to get data');
    
    return response.json();
}