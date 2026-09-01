import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { apiErrorMessage, notesApi } from '../api/notesApi';
import TagSuggestions from '../components/TagSuggestions';
import { toggleTag } from '../utils/tags';
import type { Note } from '../types';

export default function ViewNote() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [note, setNote] = useState<Note | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [summarizing, setSummarizing] = useState(false);
  const [summarizeError, setSummarizeError] = useState<string | null>(null);
  // Accepted suggestions are held here until the user saves them.
  const [tags, setTags] = useState('');
  const [savingTags, setSavingTags] = useState(false);
  const [tagsError, setTagsError] = useState<string | null>(null);

  useEffect(() => {
    notesApi
      .get(Number(id))
      .then((loaded) => {
        setNote(loaded);
        setTags(loaded.tags);
      })
      .catch(() => setError('Note not found.'))
      .finally(() => setLoading(false));
  }, [id]);

  const handleDelete = async () => {
    if (!note || !confirm('Delete this note?')) return;
    await notesApi.remove(note.id);
    navigate('/');
  };

  const handleSummarize = async () => {
    if (!note) return;
    setSummarizing(true);
    setSummarizeError(null);
    try {
      const updated = await notesApi.summarize(note.id);
      setNote(updated);
    } catch (err) {
      setSummarizeError(apiErrorMessage(err, 'Failed to generate summary.'));
    } finally {
      setSummarizing(false);
    }
  };

  const handleSaveTags = async () => {
    if (!note) return;
    setSavingTags(true);
    setTagsError(null);
    try {
      await notesApi.update(note.id, { title: note.title, content: note.content, tags });
      setNote({ ...note, tags });
    } catch (err) {
      setTagsError(apiErrorMessage(err, 'Failed to save tags.'));
    } finally {
      setSavingTags(false);
    }
  };

  if (loading) return <p>Loading note...</p>;
  if (error || !note) return <p className="error">{error ?? 'Note not found.'}</p>;

  return (
    <div>
      <Link to="/">&larr; Back to notes</Link>
      <div className="page-header">
        <h1>{note.title}</h1>
        <div className="actions">
          <Link className="button" to={`/notes/${note.id}/edit`}>
            Edit
          </Link>
          <button className="button" onClick={handleSummarize} disabled={summarizing}>
            {summarizing ? 'Summarizing...' : 'Summarize'}
          </button>
          <button className="button danger" onClick={handleDelete}>
            Delete
          </button>
        </div>
      </div>
      <p className="tags">{tags || 'No tags yet'}</p>
      <TagSuggestions
        noteId={note.id}
        tags={tags}
        onToggle={(tag) => setTags((current) => toggleTag(current, tag))}
      />
      {tags !== note.tags && (
        <div className="actions">
          <button className="button" onClick={handleSaveTags} disabled={savingTags}>
            {savingTags ? 'Saving tags...' : 'Save tags'}
          </button>
          <button className="button subtle" onClick={() => setTags(note.tags)} disabled={savingTags}>
            Discard
          </button>
        </div>
      )}
      {tagsError && <p className="error">{tagsError}</p>}
      <p className="content">{note.content}</p>
      {summarizing && <p className="loading">Generating summary...</p>}
      {summarizeError && <p className="error">{summarizeError}</p>}
      {note.summary && !summarizing && (
        <div className="summary">
          <h2>Summary</h2>
          <p>{note.summary}</p>
        </div>
      )}
    </div>
  );
}
