import {Button} from "@heroui/button";
import Link from "next/dist/client/link";

export default function NotFound() {
    return (
        <div className='flex h-full items-center justify-center'>
            <div className='text-center space-y-6'>
                <h1 className='text-5xl font-bold'>404 - Page Not Found</h1>
                <p className='text-lg text-base-content/80'>
                    Sorry, the page you are looking for doesn&#39;t exist.
                </p>
                <Button as={Link} href='/' color='primary'>Go Home</Button>
            </div>
        </div>
    );
}