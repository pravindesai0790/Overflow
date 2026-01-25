import {number, string} from "zod";

export type Question = {
    id: string
    title: string
    content: string
    askerId: string
    createdAt: string
    updatedAt?: string
    viewCount: number
    tagSlugs: string[]
    hasAcceptedAnswer: boolean
    votes: number
    answerCount: number
    answers: Answer[]
    author?: Profile
    userVoted: number
}

export type Answer = {
    id: string
    content: string
    userId: string
    createdAt: string
    updatedAt?: string
    accepted: boolean
    questionId: string
    author?: Profile
    votes: number
    userVoted: number
}

export type Tag = {
    id: string
    name: string
    slug: string
    description: string
    usageCount: number
}

export type TrendingTag = {
    tag: string
    count: number
}

export type Profile = {
    userId: string
    displayName: string
    description?: string
    reputation: number
}

export type FetchResponse<T> = {
    data: T | null,
    error?: { message: string, status: number }
}

export type VoteRecord = {
    targetId: string
    targetType: 'Question' | 'Answer'
    voteValue: number
}

export type Vote = {
    targetId: string
    targetType: 'Question' | 'Answer'
    targetUserId: string
    questionId: string
    voteValue: 1 | -1
}