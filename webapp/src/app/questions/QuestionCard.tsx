import {Question} from "@/lib/types";
import Link from "next/dist/client/link";
import {Chip} from "@heroui/chip";
import {Avatar} from "@heroui/avatar";

type Props = {
    question: Question;
}

export default function QuestionCard({question}: Props) {
    return (
        <div className='flex gap-6 px-6'>
            <div className='flex flex-col text-sm gap-3 min-w-24'>
                <div>{question.votes} {question.votes === 1 ? 'vote' : 'votes'}</div>
                <div>{question.answerCount} {question.answerCount === 1 ? 'answer' : 'answers'}</div>
                <div>{question.viewCount} {question.viewCount === 1 ? 'view' : 'views'}</div>
            </div>
            <div className='flex flex-1 justify-between min-h-32'>
                <div className='flex flex-col gap-2'>
                    <Link 
                        href={`/questions/${question.id}`}
                        className='text-primary font-semibold hover:underline first-letter:uppercase'
                    >
                        {question.title}
                    </Link>
                    <div 
                        className='line-clamp-2'
                        dangerouslySetInnerHTML={{__html: question.content}}
                    />
                    <div className='flex justify-between pt-2'>
                        <div className='flex gap-2'>
                            {question.tagSlugs.map(slug => (
                                <Chip
                                    key={slug}
                                    variant='bordered'
                                    as={Link}
                                    href={`/questions?tag=/${slug}`}
                                >
                                    {slug}
                                </Chip>
                            ))}
                        </div>
                        
                        <div className='text-sm flex items-center gap-2'>
                            <Avatar 
                                className='h-6 w-6'
                                color='secondary'
                                name={question.askerDisplayName.charAt(0)}
                            />
                            <Link href={`/profiles/${question.askerId}`}>
                                {question.askerDisplayName}
                            </Link>
                            <span>asked {question.createdAt}</span>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}