import {z} from "zod";

const required = (name: string) => z.string().trim().min(1, 
    {message: `${name} is required`});

export const questionSchema = z.object({
    title: required("Title"),
    content: required("Content").min(10, 
        {message: 'Content should be at least 10 characters'}),
    tags: z.array(z.string(), {message: 'Select at least 1 tag'})
        .min(1, {message: 'Select at least 1 tag'})
        .max(5, {message: 'No more than 5 tags can be selected'}),
});

export type QuestionSchema = z.infer<typeof questionSchema>

