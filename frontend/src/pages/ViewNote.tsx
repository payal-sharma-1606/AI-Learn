import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { apiErrorMessage, notesApi } from '../api/notesApi';
import type { Note } from '../types';

export default function ViewNote() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [note, setNote] = useState<Note | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [summarizing, setSummarizing] = useState(false);
  const [summarizeError, setSummarizeError] = useState<string | null>(null);

  useEffect(() => {
    notesApi
      .get(Number(id))
      .then(setNote)
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
      <p className="tags">{note.tags}</p>
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
