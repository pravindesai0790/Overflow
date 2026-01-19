'use server'

import {Question} from "@/lib/types";
import {fetchClient} from "@/lib/fetchClient";
import {QuestionSchema} from "@/lib/schemas/questionSchema";

export async function getQuestions(tag?: string) {
    let url = '/questions';
    if (tag) url += `?tag=${tag}`;
    return fetchClient<Question[]>(url, 'GET');
}

export async function getQuestionById(id: string) {
    return fetchClient<Question>(`/questions/${id}`, 'GET');
}

export async function searchQuestions(query: string) {
    return fetchClient<Question[]>(`/search?query=${query}`, 'GET');
}

export async function postQuestion(question: QuestionSchema) {
    return fetchClient<Question>('/questions', 'POST', {body: question});
}

export async function updateQuestion(question: QuestionSchema, id: string) {
    return fetchClient(`/questions/${id}`, 'PUT', {body: question});
}