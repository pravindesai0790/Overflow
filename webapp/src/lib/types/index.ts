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
}

export type Answer = {
    id: string
    content: string
    userId: string
    createdAt: string
    updatedAt?: string
    accepted: boolean
    questionId: string
    author: Profile
}

export type Tag = {
    id: string
    name: string
    slug: string
    description: string
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
