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
