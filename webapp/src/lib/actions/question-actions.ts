'use server'

import {Question} from "@/lib/types";
import {fetchClient} from "@/lib/fetchClient";

export async function getQuestions(tag?: string): Promise<Question[]> {
    let url = '/questions';
    //if (tag) url += `?tag=${tag}`;
    if (tag)
    {
        // Remove all '/' characters from the value
        tag = tag.replace(/\//g, '');
        url += `?tag=${tag}`;
    }
    return fetchClient<Question[]>(url, 'GET');
}

export async function getQuestionById(id: string): Promise<Question> {
    return fetchClient<Question>(`/questions/${id}`, 'GET');
}