import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { notesApi } from '../api/notesApi';
import type { Note } from '../types';

export default function ViewNote() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [note, setNote] = useState<Note | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

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
          <button className="button danger" onClick={handleDelete}>
            Delete
          </button>
        </div>
      </div>
      <p className="tags">{note.tags}</p>
      <p className="content">{note.content}</p>
      {note.summary && (
        <div className="summary">
          <h2>Summary</h2>
          <p>{note.summary}</p>
        </div>
      )}
    </div>
  );
}
