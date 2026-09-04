import { EditorContent, useEditor } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import {
  FontSize,
  TextStyle,
} from "@tiptap/extension-text-style";

interface EditorEmailProps {
  value: string;
  onChange: (html: string) => void;
}

export function EditorEmail({
  value,
  onChange,
}: EditorEmailProps) {
  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: false,
        blockquote: false,
        code: false,
        codeBlock: false,
        horizontalRule: false,
        strike: false,
        link: false,
      }),
      TextStyle,
      FontSize,
    ],

    content: value || "<p></p>",

    onUpdate: ({ editor }) => {
      onChange(editor.getHTML());
    },
  });

  if (!editor) {
    return null;
  }

  return (
    <div className="email-editor">
      {/* Barra de ferramentas */}
      <div className="editor-toolbar">
        <button
          type="button"
          title="Negrito"
          className={editor.isActive("bold") ? "active" : ""}
          onClick={() =>
            editor.chain().focus().toggleBold().run()
          }
        >
          <strong>B</strong>
        </button>

        <button
          type="button"
          title="Itálico"
          className={editor.isActive("italic") ? "active" : ""}
          onClick={() =>
            editor.chain().focus().toggleItalic().run()
          }
        >
          <em>I</em>
        </button>

        <button
          type="button"
          title="Sublinhado"
          className={editor.isActive("underline") ? "active" : ""}
          onClick={() =>
            editor.chain().focus().toggleUnderline().run()
          }
        >
          <u>U</u>
        </button>

        <div className="toolbar-divider" />

        <select
          aria-label="Tamanho da fonte"
          defaultValue=""
          onChange={(event) => {
            const tamanho = event.target.value;

            if (!tamanho) {
              editor
                .chain()
                .focus()
                .unsetFontSize()
                .run();

              return;
            }

            editor
              .chain()
              .focus()
              .setFontSize(tamanho)
              .run();
          }}
        >
          <option value="">Tamanho</option>
          <option value="12px">12</option>
          <option value="14px">14</option>
          <option value="16px">16</option>
          <option value="18px">18</option>
          <option value="20px">20</option>
          <option value="24px">24</option>
        </select>

        <div className="toolbar-divider" />

        <button
          type="button"
          title="Lista com marcadores"
          className={
            editor.isActive("bulletList")
              ? "active"
              : ""
          }
          onClick={() =>
            editor
              .chain()
              .focus()
              .toggleBulletList()
              .run()
          }
        >
          • Lista
        </button>

        <button
          type="button"
          title="Lista numerada"
          className={
            editor.isActive("orderedList")
              ? "active"
              : ""
          }
          onClick={() =>
            editor
              .chain()
              .focus()
              .toggleOrderedList()
              .run()
          }
        >
          1. Lista
        </button>

        <div className="toolbar-divider" />

        <button
          type="button"
          title="Desfazer"
          disabled={!editor.can().undo()}
          onClick={() =>
            editor.chain().focus().undo().run()
          }
        >
          ↶
        </button>

        <button
          type="button"
          title="Refazer"
          disabled={!editor.can().redo()}
          onClick={() =>
            editor.chain().focus().redo().run()
          }
        >
          ↷
        </button>
      </div>

      {/* Área de edição */}
      <EditorContent editor={editor} />
    </div>
  );
}