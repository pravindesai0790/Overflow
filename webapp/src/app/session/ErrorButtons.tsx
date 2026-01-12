'use client'

import {Button} from "@heroui/button";
import {triggerError} from "@/lib/actions/error-actions";
import {useTransition} from "react";

export default function ErrorButtons() {
    const [pending, startTransition] = useTransition();
    
    const onClick = (code: number) => {
        startTransition(async () => {
            await triggerError(code);
        })
    }
    
    return (
        <div className='flex gap-6 items-center justify-center mt-6 w-full'>
            {[400,401,403,404,500].map(code => (
                <Button
                    onPress={() => onClick(code)}
                    color='primary'
                    key={code}
                    type='button'
                >
                    Test {code}
                </Button>
            ))}
        </div>
    );
}