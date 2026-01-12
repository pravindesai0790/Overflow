import {getQuestions} from "@/lib/actions/question-actions";
import QuestionCard from "@/app/questions/QuestionCard";
import QuestionsHeader from "@/app/questions/QuestionsHeader";

export default async function QuestionsPage({searchParams}: {searchParams?: Promise<{tag?:string}>}) {
    const params = await  searchParams;
    const {data: questions, error} = await getQuestions(params?.tag?.replace(/\//g, ''));
    
    if (error) throw error; // it will be captured by error boundary page in client (i.e, error.tsx page)
    
    return (
        <>
            <QuestionsHeader total={questions?.length || 0} tag={params?.tag?.replace(/\//g, '')}/>
            {questions?.map(question => (
                <div key={question.id} className='py-4 not-last:border-b w-full flex'>
                    <QuestionCard key={question.id} question={question} />
                </div>
            ))}
        </>
    );
}