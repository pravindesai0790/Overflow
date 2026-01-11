import {Answer} from "@/lib/types";
import VotingButtons from "@/app/questions/[id]/VotingButtons";

type Props = {
    answer: Answer;
}

export default function AnswerContent({answer}: Props) {
    return (
        <div className='flex border-b pb-3 px-6'>
            <VotingButtons accepted={answer.accepted}/>
            <div
                className='flex-1 mt-4 ml-6 prose max-w-none dark:prose-invert'
                dangerouslySetInnerHTML={{__html: answer.content}}
            />
        </div>
    );
}