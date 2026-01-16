import {EditorContent, useEditor} from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import MenuBar from "@/components/rte/MenuBar";
import {useEffect} from "react";
import clsx from "clsx";

type Props = {
    onChange: (body: string) => void;
    onBlur: () => void;
    value: string;
    errorMessage?: string;
}

export default function RichTextEditor({onChange, onBlur, value, errorMessage}: Props) {
    const editor = useEditor({
        extensions: [StarterKit],
        content: '',
        editorProps: {
            attributes: {
                class: clsx('w-full p-3 bg-default-100 rounded-xl min-h-60 prose dark:prose-invert max-w-none ' +
                    'dark:prose-pre:bg-primary-100', {
                    'bg-red-50 dark:bg-red-900/30': errorMessage
                }, {'hover:bg-red-900/30': errorMessage})
            }
        },
        onBlur() {
            onBlur()
        },
        onUpdate({editor}) {
            onChange(editor.getHTML())
        },
        immediatelyRender: false
    });
    
    useEffect(() => {
        if(editor && value !== editor.getHTML()) {
            editor.commands.setContent(value);
        }
    }, [editor, value]);
    
    return (
        <div>
            <MenuBar editor={editor} />
            <EditorContent editor={editor} />
        </div>
    );
}