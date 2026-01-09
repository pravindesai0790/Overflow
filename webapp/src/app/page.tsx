import {Button} from "@heroui/button";
import {AcademicCapIcon} from "@heroicons/react/24/solid";

export default function Home() {
  return (
    <div className='text-4xl text-red-500'>
      <h1>Overflow app</h1>
      <Button color='primary' endContent={<AcademicCapIcon className='size-6' />}>
        Click me!
      </Button>
    </div>
  );
}
