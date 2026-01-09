'use client';

import {HeroUIProvider} from "@heroui/react";
import {ReactNode} from "react";
import {useRouter} from "next/dist/client/components/navigation";

export default function Providers({children}: {children: ReactNode}) {
    const router = useRouter();
    
    return (
        <HeroUIProvider navigate={router.push} className='flex flex-col h-full'>
            {children}
        </HeroUIProvider>
    );
}