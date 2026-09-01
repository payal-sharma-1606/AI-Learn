import axios from 'axios';
import type { Note, NoteInput, TagSuggestionResponse } from '../types';

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  // Slightly longer than the server's own 60s AI timeout, so the server's
  // error message wins rather than the client giving up first.
  timeout: 75_000,
});

/** Prefers the ProblemDetails message from the API over a generic fallback. */
export function apiErrorMessage(error: unknown, fallback: string): string {
  if (axios.isAxiosError(error)) {
    if (error.code === 'ECONNABORTED') {
      return 'The request timed out. Please try again.';
    }
    const detail = (error.response?.data as { detail?: string } | undefined)?.detail;
    if (detail) {
      return detail;
    }
    if (!error.response) {
      return 'Could not reach the server. Is the API running?';
    }
  }
  return fallback;
}

export const notesApi = {
  list: () => client.get<Note[]>('/notes').then((res) => res.data),
  get: (id: number) => client.get<Note>(`/notes/${id}`).then((res) => res.data),
  create: (note: NoteInput) => client.post<Note>('/notes', note).then((res) => res.data),
  update: (id: number, note: NoteInput) => client.put(`/notes/${id}`, note),
  remove: (id: number) => client.delete(`/notes/${id}`),
  summarize: (id: number) => client.post<Note>(`/notes/${id}/summarize`).then((res) => res.data),
  suggestTags: (id: number) =>
    client.post<TagSuggestionResponse>(`/notes/${id}/suggest-tags`).then((res) => res.data.tags),
};
