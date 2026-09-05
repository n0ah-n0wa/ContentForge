const ALLOWED_TAGS = new Set([
  'a',
  'b',
  'blockquote',
  'br',
  'code',
  'div',
  'em',
  'h1',
  'h2',
  'h3',
  'h4',
  'h5',
  'h6',
  'hr',
  'i',
  'li',
  'ol',
  'p',
  'pre',
  'span',
  'strong',
  'sub',
  'sup',
  'u',
  'ul',
]);

const ALLOWED_ATTRIBUTES: Record<string, Set<string>> = {
  a: new Set(['href', 'title', 'target', 'rel']),
  '*': new Set(['class']),
};

const UNSAFE_URL_PATTERN = /^(?:javascript|data|vbscript):/i;

function isAllowedAttribute(tagName: string, attributeName: string): boolean {
  const globalAttributes = ALLOWED_ATTRIBUTES['*'] ?? new Set<string>();
  if (globalAttributes.has(attributeName)) {
    return true;
  }

  const tagAttributes = ALLOWED_ATTRIBUTES[tagName];
  return tagAttributes?.has(attributeName) ?? false;
}

function sanitizeUrl(value: string): string | null {
  const trimmed = value.trim();
  if (!trimmed || UNSAFE_URL_PATTERN.test(trimmed)) {
    return null;
  }

  // Protocol-relative URLs can escape the app origin unexpectedly.
  if (trimmed.startsWith('//')) {
    return null;
  }

  return trimmed;
}

function sanitizeNode(node: Node, output: ParentNode): void {
  if (node.nodeType === Node.TEXT_NODE) {
    output.appendChild(node.cloneNode(true));
    return;
  }

  if (node.nodeType !== Node.ELEMENT_NODE) {
    return;
  }

  const element = node as HTMLElement;
  const tagName = element.tagName.toLowerCase();
  if (!ALLOWED_TAGS.has(tagName)) {
    for (const child of Array.from(element.childNodes)) {
      sanitizeNode(child, output);
    }
    return;
  }

  const sanitized = document.createElement(tagName);
  for (const attribute of Array.from(element.attributes)) {
    const name = attribute.name.toLowerCase();
    if (!isAllowedAttribute(tagName, name)) {
      continue;
    }

    if (name === 'href' || name === 'src') {
      const safeUrl = sanitizeUrl(attribute.value);
      if (safeUrl) {
        sanitized.setAttribute(name, safeUrl);
      }
      continue;
    }

    if (name.startsWith('on')) {
      continue;
    }

    sanitized.setAttribute(name, attribute.value);
  }

  if (tagName === 'a' && sanitized.getAttribute('target') === '_blank') {
    sanitized.setAttribute('rel', 'noopener noreferrer');
  }

  for (const child of Array.from(element.childNodes)) {
    sanitizeNode(child, sanitized);
  }

  output.appendChild(sanitized);
}

/** Removes unsafe markup from rich text before storage or rendering. */
export function sanitizeRichText(html: string): string {
  if (!html.trim()) {
    return '';
  }

  const template = document.createElement('template');
  template.innerHTML = html;

  const fragment = document.createDocumentFragment();
  for (const child of Array.from(template.content.childNodes)) {
    sanitizeNode(child, fragment);
  }

  const container = document.createElement('div');
  container.appendChild(fragment);
  return container.innerHTML;
}

/** Strips all HTML tags, leaving plain text for previews or search. */
export function richTextToPlainText(html: string): string {
  const sanitized = sanitizeRichText(html);
  const container = document.createElement('div');
  container.innerHTML = sanitized;
  return container.textContent ?? '';
}
