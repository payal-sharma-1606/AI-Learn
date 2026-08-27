import axios from 'axios';
import type { Note, NoteInput } from '../types';

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
});

export const notesApi = {
  list: () => client.get<Note[]>('/notes').then((res) => res.data),
  get: (id: number) => client.get<Note>(`/notes/${id}`).then((res) => res.data),
  create: (note: NoteInput) => client.post<Note>('/notes', note).then((res) => res.data),
  update: (id: number, note: NoteInput) =>
    client.put(`/notes/${id}`, { id, ...note }),
  remove: (id: number) => client.delete(`/notes/${id}`),
  summarize: (id: number) => client.post<Note>(`/notes/${id}/summarize`).then((res) => res.data),
};
