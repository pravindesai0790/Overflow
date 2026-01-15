import {EditorContent, useEditor} from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import MenuBar from "@/components/rte/MenuBar";

export default function RichTextEditor() {
    const editor = useEditor({
        extensions: [StarterKit],
        content: '<p>Hello overflow</p>',
        editorProps: {
            attributes: {
                class: 'w-full p-3 bg-default-100 rounded-xl min-h-60 prose dark:prose-invert max-w-none ' +
                    'dark:prose-pre:bg-primary-100'
            }
        },
        immediatelyRender: false
    });
    
    return (
        <div>
            <MenuBar editor={editor} />
            <EditorContent editor={editor} />
        </div>
    );
}