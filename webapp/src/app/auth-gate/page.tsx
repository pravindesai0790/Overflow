'use client'

import {Button} from "@heroui/button";
import {signIn} from "next-auth/react";

export default function AuthGatePage() {
    const searchParams = new URLSearchParams();
    const callbackUrl = searchParams.get("callbackUrl");
    
    return (
        <div className='flex h-full items-center justify-center'>
            <div className='text-center space-y-6'>
                <h1 className='text-5xl font-bold'>Please login</h1>
                <p className='text-lg text-foreground-500'>
                    You need to be logged in to do that.
                </p>
                <Button 
                    onPress={() => signIn('keycloak', {redirectTo: callbackUrl || '/questions'})} 
                    color='primary'>Login
                </Button>
            </div>
        </div>
    );
}