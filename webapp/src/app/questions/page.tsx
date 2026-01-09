import {getQuestions} from "@/lib/actions/question-actions";
import QuestionCard from "@/app/questions/QuestionCard";

export default async function QuestionsPage() {
    const questions = await getQuestions();
    
    return (
        <>
            {questions.map(question => (
                <div key={question.id} className='py-4 not-last:border-b w-full flex'>
                    <QuestionCard key={question.id} question={question} />
                </div>
            ))}
        </>
    );
}