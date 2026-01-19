import { Button } from "@heroui/react";
import {useTransition} from "react";
import {useRouter} from "next/dist/client/components/navigation";

type Props = {
    questionId: string;
}

export default function DeleteQuestionButton({ questionId }: Props) {
    const [pending, startTransition] = useTransition();
    const router = useRouter(); 
    
    return (
        <Button
            size='sm'
            variant='faded'
            color='danger'
        >
            Delete
        </Button>
    );
}