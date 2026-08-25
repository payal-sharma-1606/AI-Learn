import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { notesApi } from '../api/notesApi';
import type { Note } from '../types';

export default function NotesList() {
  const [notes, setNotes] = useState<Note[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    notesApi
      .list()
      .then(setNotes)
      .catch(() => setError('Failed to load notes.'))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <p>Loading notes...</p>;
  if (error) return <p className="error">{error}</p>;

  return (
    <div>
      <div className="page-header">
        <h1>Notes</h1>
        <Link className="button" to="/notes/new">
          New Note
        </Link>
      </div>
      {notes.length === 0 ? (
        <p>No notes yet. Create your first one.</p>
      ) : (
        <ul className="note-list">
          {notes.map((note) => (
            <li key={note.id}>
              <Link to={`/notes/${note.id}`}>{note.title}</Link>
              <span className="tags">{note.tags}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
