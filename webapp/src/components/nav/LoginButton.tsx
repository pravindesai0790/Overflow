'use client';

import {Button} from "@heroui/button";
import {signIn} from "next-auth/react";

export default function LoginButton() {
    return (
        <Button 
            color='secondary' 
            variant='bordered'
            onPress={() => signIn('keycloak', {redirectTo: '/questions'})}
        >
            Login
        </Button>
    );
}