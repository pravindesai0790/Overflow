import {Button} from "@heroui/button";
import {ArrowDownCircleIcon, ArrowUpCircleIcon} from "@heroicons/react/24/outline";

export default function VotingButtons() {
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
        </div>
    );
}