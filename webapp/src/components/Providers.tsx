'use client';

import {HeroUIProvider} from "@heroui/react";
import {ReactNode} from "react";

export default function Providers({children}: {children: ReactNode}) {
    return (
        <HeroUIProvider className='flex flex-col h-full'>
            {children}
        </HeroUIProvider>
    );
}