import axe, { type Result } from 'axe-core';
import { expect } from 'vitest';

const defaultRules = {
  runOnly: {
    type: 'tag' as const,
    values: ['wcag2a', 'wcag2aa', 'wcag21aa'],
  },
};

function formatViolations(violations: Result[]): string {
  return violations
    .map((violation) => {
      const nodes = violation.nodes
        .map((node) => `  - ${node.target.join(' ')}: ${node.failureSummary}`)
        .join('\n');
      return `${violation.id} (${violation.impact}): ${violation.help}\n${nodes}`;
    })
    .join('\n\n');
}

/** Assert an element tree has no serious WCAG 2.1 A/AA axe violations. */
export async function expectNoA11yViolations(
  container: HTMLElement,
  options?: { exclude?: string[] },
): Promise<void> {
  const results = await axe.run(container, {
    ...defaultRules,
    exclude: options?.exclude?.map((selector) => [selector]),
  });

  expect(results.violations, formatViolations(results.violations)).toEqual([]);
}
