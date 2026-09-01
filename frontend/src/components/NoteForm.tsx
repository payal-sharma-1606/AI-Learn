import { useState } from 'react';
import { apiErrorMessage } from '../api/notesApi';
import TagSuggestions from './TagSuggestions';
import { toggleTag } from '../utils/tags';
import type { NoteInput } from '../types';

interface NoteFormProps {
  initial?: NoteInput;
  submitLabel: string;
  onSubmit: (note: NoteInput) => Promise<void>;
  /** Set when editing an existing note; enables AI tag suggestions. */
  noteId?: number;
}

export default function NoteForm({ initial, submitLabel, onSubmit, noteId }: NoteFormProps) {
  const [title, setTitle] = useState(initial?.title ?? '');
  const [content, setContent] = useState(initial?.content ?? '');
  const [tags, setTags] = useState(initial?.tags ?? '');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !content.trim()) {
      setError('Title and content are required.');
      return;
    }
    setError(null);
    setSaving(true);
    try {
      await onSubmit({ title, content, tags });
    } catch (err) {
      setError(apiErrorMessage(err, 'Failed to save note.'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <form className="note-form" onSubmit={handleSubmit}>
      {error && <p className="error">{error}</p>}
      <label>
        Title
        <input value={title} onChange={(e) => setTitle(e.target.value)} />
      </label>
      <label>
        Content
        <textarea rows={10} value={content} onChange={(e) => setContent(e.target.value)} />
      </label>
      <label>
        Tags (comma-separated)
        <input value={tags} onChange={(e) => setTags(e.target.value)} />
      </label>
      {noteId === undefined ? (
        <p className="hint">Save the note first to get AI tag suggestions.</p>
      ) : (
        <TagSuggestions
          noteId={noteId}
          tags={tags}
          onToggle={(tag) => setTags((current) => toggleTag(current, tag))}
        />
      )}
      <button className="button" type="submit" disabled={saving}>
        {saving ? 'Saving...' : submitLabel}
      </button>
    </form>
  );
}
