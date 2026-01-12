'use client';

import {HeroUIProvider, ToastProvider} from "@heroui/react";
import {ReactNode} from "react";
import {useRouter} from "next/dist/client/components/navigation";
import {ThemeProvider} from "next-themes";

export default function Providers({children}: {children: ReactNode}) {
    const router = useRouter();
    
    return (
        <HeroUIProvider navigate={router.push} className='flex flex-col h-full'>
            <ToastProvider />
            <ThemeProvider
                attribute='class'
                defaultTheme='light'
            >
                {children}
            </ThemeProvider>
        </HeroUIProvider>
    );
}