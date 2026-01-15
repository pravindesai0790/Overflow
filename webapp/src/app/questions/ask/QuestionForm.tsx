'use client';

import {useTagStore} from "@/lib/useTagStore";
import {Form} from "@heroui/form";
import {Input, Textarea} from "@heroui/input";
import {Select, SelectItem} from "@heroui/select";
import {Button} from "@heroui/button";
import {useForm} from "react-hook-form";
import {QuestionSchema} from "@/lib/schemas/questionSchema";

export default function QuestionForm() {
    const tags = useTagStore(state => state.tags);
    const {register, handleSubmit, formState: {isSubmitting, isValid, errors}} = useForm<QuestionSchema>();
    
    const onSubmit = (data: QuestionSchema) => {
        console.log(isSubmitting, isValid, data);
    }
    
    return (
        <Form onSubmit={handleSubmit(onSubmit)} className='flex flex-col gap-3 p-6 shadow-xl bg-white dark:bg-black'>
            <div className='flex flex-col gap-3 w-full'>
                <h3 className='text-2xl font-semibold'>Title</h3>
                <Input
                    {...register('title')}
                    type='text'
                    className='w-full'
                    label='Be specific and imagine you are asking a question to another person'
                    labelPlacement='outside-top'
                    placeholder='e.g how would you truncate text in tailwinf'
                />
            </div>
            <div className='flex flex-col gap-3 w-full'>
                <h3 className='text-2xl font-semibold'>Body</h3>
                <Textarea
                    {...register('content')}
                    className='w-full'
                    label='Include all the information someone would need to answer your question'
                    labelPlacement='outside-top'
                    minRows={12}
                />
            </div>
            <div className='flex flex-col gap-3 w-full'>
                <h3 className='text-2xl font-semibold'>Tags</h3>
                <p className='text-sm'>Add up to 5 tags to describe what your question is about</p>
                <Select
                    {...register('tags')}
                    className='w-full'
                    label='Select 1-5 tags'
                    selectionMode='multiple'
                    isClearable
                    disallowEmptySelection
                    items={tags}
                >
                    {(tag) => <SelectItem key={tag.id}>{tag.name}</SelectItem> }
                </Select>
            </div>
            <Button 
                type='submit'
                color='primary' 
                className='w-fit'
            >
                Post your question
            </Button>
        </Form>
    );
}
