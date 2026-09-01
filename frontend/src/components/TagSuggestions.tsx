import { useState } from 'react';
import { apiErrorMessage, notesApi } from '../api/notesApi';
import { hasTag } from '../utils/tags';

interface TagSuggestionsProps {
  /** Suggestions come from saved content, so the note must already exist. */
  noteId: number;
  /** Current comma-separated tags, used to show which suggestions are accepted. */
  tags: string;
  onToggle: (tag: string) => void;
}

export default function TagSuggestions({ noteId, tags, onToggle }: TagSuggestionsProps) {
  const [suggestions, setSuggestions] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [empty, setEmpty] = useState(false);

  const handleSuggest = async () => {
    setLoading(true);
    setError(null);
    setEmpty(false);
    try {
      const suggested = await notesApi.suggestTags(noteId);
      setSuggestions(suggested);
      setEmpty(suggested.length === 0);
    } catch (err) {
      setError(apiErrorMessage(err, 'Failed to suggest tags.'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="tag-suggestions">
      <button className="button subtle" type="button" onClick={handleSuggest} disabled={loading}>
        {loading ? 'Thinking...' : suggestions.length > 0 ? 'Suggest again' : 'Suggest tags with AI'}
      </button>
      {error && <p className="error">{error}</p>}
      {empty && <p className="loading">The AI did not suggest any tags for this note.</p>}
      {suggestions.length > 0 && (
        <>
          <p className="hint">Click a suggestion to add or remove it.</p>
          <div className="chips">
            {suggestions.map((tag) => {
              const accepted = hasTag(tags, tag);
              return (
                <button
                  key={tag}
                  type="button"
                  className={accepted ? 'chip accepted' : 'chip'}
                  aria-pressed={accepted}
                  onClick={() => onToggle(tag)}
                >
                  {accepted ? `✓ ${tag}` : `+ ${tag}`}
                </button>
              );
            })}
          </div>
        </>
      )}
    </div>
  );
}
