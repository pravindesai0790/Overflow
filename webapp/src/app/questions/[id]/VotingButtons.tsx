'use client'

import {Button} from "@heroui/button";
import {ArrowDownCircleIcon, ArrowUpCircleIcon, CheckCircleIcon as CheckOutlined} from "@heroicons/react/24/outline";
import {CheckCircleIcon as CheckSolid} from "@heroicons/react/24/solid";
import {Answer, Question} from "@/lib/types";
import {useTransition} from "react";
import {acceptedAnswer} from "@/lib/actions/question-actions";
import {handleError, successToast} from "@/lib/util";

type Props = {
    target: Question | Answer;
    currentUserId?: string;
    askerId?: string;
}

const isTargetAnswer = (target: Question | Answer): target is Answer => {
    return "questionId" in target;
}

export default function VotingButtons({target, currentUserId, askerId}: Props) {
    const [pending, startTransition] = useTransition();
    const isAnswer = isTargetAnswer(target);
    
    const handleAcceptAnswer = () => {
        if (!isAnswer || askerId !== currentUserId) return;
        startTransition(async () => {
            const {error} = await acceptedAnswer(target.id, target.questionId);
            if (error) handleError(error);
            else successToast('Answer has been accepted.', 'success');
        })
    }
    
    return (
        <div className='shrink-0 flex flex-col gap-3 items-center justify-start mt-4'>
            <Button
                isIconOnly
                variant='light'
            >
                <ArrowUpCircleIcon className='w-12'/>
            </Button>
            <span className='text-xl font-semibold'>0</span>
            <Button
                isIconOnly
                variant='light'
            >
                <ArrowDownCircleIcon className='w-12'/>
            </Button>
            {isAnswer && (
                <Button
                    isIconOnly
                    variant='light'
                    className='disabled:opacity-100'
                    isDisabled={target.accepted || askerId !== currentUserId}
                    isLoading={pending}
                    onPress={handleAcceptAnswer}
                >
                    {target.accepted ? (
                        <CheckSolid className='text-success' />
                    ) : (
                        <CheckOutlined className='size-12 text-default-500'/>
                    )}
                </Button>
            )}
        </div>
    );
}