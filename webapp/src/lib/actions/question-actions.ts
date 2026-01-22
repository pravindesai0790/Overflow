'use server'

import {Answer, FetchResponse, Profile, Question} from "@/lib/types";
import {fetchClient} from "@/lib/fetchClient";
import {QuestionSchema} from "@/lib/schemas/questionSchema";
import {AnswerSchema} from "@/lib/schemas/answerSchema";
import {revalidatePath} from "next/dist/server/web/spec-extension/revalidate";

export async function getQuestions(tag?: string) : Promise<FetchResponse<Question[]>> {
    let questionUrl = '/questions';
    if (tag) questionUrl += `?tag=${tag}`;
    
    const {data: questions, error: questionError} = await fetchClient<Question[]>(questionUrl, 'GET');

    if(!questions || questionError) {
        return {
            data: null,
            error: {message: 'Problem getting questions', status: 500}
        }
    }
    
    const userIds = Array.from(new Set(questions.map(x => x.askerId)));
    if (userIds.length === 0) return {data: []};
    
    const ids = Array.from(userIds).sort();
    const profilesUrl = '/profiles/batch?' + new URLSearchParams({ids: ids.join(',')});
    const {data: profiles, error: profilesError} = await fetchClient<Profile[]>(profilesUrl, 'GET', 
        {cache: "force-cache", next: {revalidate: 300}});
    
    if (profilesError) return {data: null, error: {message: 'Problem getting profiles', status: 500}};
    
    const profileMap = new Map(profiles?.map(p => [p.userId, p]));
    
    const enriched = questions.map(q => ({
        ...q,
        author: profileMap.get(q.askerId)
    }));
    
    return {data: enriched}
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

export async function deleteQuestion(id: string) {
    return fetchClient(`/questions/${id}`, 'DELETE');
}

export async function postAnswer(data: AnswerSchema, questionId: string) {
    const result = await fetchClient<Answer>(`/questions/${questionId}/answers`, 'POST', {body: data});
    
    revalidatePath(`/questions/${questionId}`);
    
    return result;
}

export async function editAnswer(answerId: string, questionId: string, content: AnswerSchema) {
    const result = await fetchClient(`/questions/${questionId}/answers/${answerId}`, 'PUT', {body: content});
    revalidatePath(`/questions/${questionId}`);
    return result;
}

export async function deleteAnswer(answerId: string, questionId: string) {
    const result = await fetchClient(`/questions/${questionId}/answers/${answerId}`, 'DELETE');
    revalidatePath(`/questions/${questionId}`);
    return result;
}