import {EditorContent, useEditor} from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import MenuBar from "@/components/rte/MenuBar";
import {useEffect} from "react";

type Props = {
    onChange: (body: string) => void;
    onBlur: () => void;
    value: string;
}

export default function RichTextEditor({onChange, onBlur, value}: Props) {
    const editor = useEditor({
        extensions: [StarterKit],
        content: '',
        editorProps: {
            attributes: {
                class: 'w-full p-3 bg-default-100 rounded-xl min-h-60 prose dark:prose-invert max-w-none ' +
                    'dark:prose-pre:bg-primary-100'
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