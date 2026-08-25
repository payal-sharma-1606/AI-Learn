import { useNavigate } from 'react-router-dom';
import { notesApi } from '../api/notesApi';
import NoteForm from '../components/NoteForm';
import type { NoteInput } from '../types';

export default function CreateNote() {
  const navigate = useNavigate();

  const handleSubmit = async (note: NoteInput) => {
    const created = await notesApi.create(note);
    navigate(`/notes/${created.id}`);
  };

  return (
    <div>
      <h1>New Note</h1>
      <NoteForm submitLabel="Create Note" onSubmit={handleSubmit} />
    </div>
  );
}
