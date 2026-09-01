/** Tags live in the API as one comma-separated string; the UI works with lists. */

export function parseTags(tags: string): string[] {
  return tags
    .split(',')
    .map((tag) => tag.trim())
    .filter((tag) => tag.length > 0);
}

export function formatTags(tags: string[]): string {
  return tags.join(', ');
}

/** Matching is case-insensitive so 'DotNet' and 'dotnet' are not both added. */
export function hasTag(tags: string, tag: string): boolean {
  const target = tag.trim().toLowerCase();
  return parseTags(tags).some((existing) => existing.toLowerCase() === target);
}

/** Adds the tag if missing, removes it if already there. */
export function toggleTag(tags: string, tag: string): string {
  const target = tag.trim();
  if (hasTag(tags, target)) {
    return formatTags(
      parseTags(tags).filter((existing) => existing.toLowerCase() !== target.toLowerCase()),
    );
  }
  return formatTags([...parseTags(tags), target]);
}
