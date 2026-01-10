import {getQuestionById} from "@/lib/actions/question-actions";
import {notFound} from "next/dist/client/components/not-found";
import QuestionDetailedHeader from "@/app/questions/[id]/QuestionDetailedHeader";

type Params = Promise<{id: string}>

export default async function QuestionDetailedPage({params}: {params: Params}) {
    const {id} = await params;
    const question = await getQuestionById(id);
    
    if (!question) return notFound();
    
    return (
        <div className='w-full'>
            <QuestionDetailedHeader question={question} />
        </div>
    );
}