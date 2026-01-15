import {EditorContent, useEditor} from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";

export default function RichTextEditor() {
    const editor = useEditor({
        extensions: [StarterKit],
        content: '<p>Hello overflow</p>',
        editorProps: {
            attributes: {
                class: 'w-full p-3 bg-default-100 rounded-xl min-h-60 prose dark:prose-invert max-w-none'
            }
        },
        immediatelyRender: false
    });
    
    return (
        <EditorContent editor={editor} />
    );
}