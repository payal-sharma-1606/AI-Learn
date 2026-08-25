import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { notesApi } from '../api/notesApi';
import NoteForm from '../components/NoteForm';
import type { Note, NoteInput } from '../types';

export default function EditNote() {
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

  const handleSubmit = async (updated: NoteInput) => {
    await notesApi.update(Number(id), updated);
    navigate(`/notes/${id}`);
  };

  if (loading) return <p>Loading note...</p>;
  if (error || !note) return <p className="error">{error ?? 'Note not found.'}</p>;

  return (
    <div>
      <h1>Edit Note</h1>
      <NoteForm
        initial={{ title: note.title, content: note.content, tags: note.tags }}
        submitLabel="Save Changes"
        onSubmit={handleSubmit}
      />
    </div>
  );
}
