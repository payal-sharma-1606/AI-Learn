export interface Note {
  id: number;
  title: string;
  content: string;
  tags: string;
  summary: string | null;
  createdAt: string;
  updatedAt: string;
}

export type NoteInput = Pick<Note, 'title' | 'content' | 'tags'>;

/** Response of POST /api/notes/{id}/suggest-tags. Suggestions are never auto-saved. */
export interface TagSuggestionResponse {
  tags: string[];
}
