import {getQuestions} from "@/lib/actions/question-actions";

export default async function QuestionsPage() {
    const questions = await getQuestions();
    
    return (
        <div>
            <ul>
                {questions.map(question => (
                    <li key={question.id}>{question.title}</li>
                ))}
            </ul>
        </div>
    );
}