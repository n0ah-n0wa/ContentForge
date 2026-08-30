import { computed, ref, type Ref } from 'vue';
import type { ContentEntry, ContentFieldValue } from '@/types/contentEntries';
import type { ContentType, ContentTypeField } from '@/types/contentTypes';
import {
  createEmptyEntryData,
  normalizeEntryData,
  snapshotsEqual,
  validateEntryClientSide,
} from '@/utils/contentEntryValidation';

export interface ContentEntryFormSnapshot {
  slug: string;
  changeSummary: string;
  data: Record<string, ContentFieldValue>;
  concurrencyToken: number;
}

export function useContentEntryForm(contentType: Ref<ContentType | null>) {
  const slug = ref('');
  const changeSummary = ref('');
  const data = ref<Record<string, ContentFieldValue>>({});
  const concurrencyToken = ref(1);
  const savedSnapshot = ref<ContentEntryFormSnapshot | null>(null);
  const clientErrors = ref<Record<string, string[]>>({});
  const serverErrors = ref<Record<string, string[]>>({});

  const sortedFields = computed<ContentTypeField[]>(() => {
    if (!contentType.value) {
      return [];
    }

    return [...contentType.value.fields].sort((left, right) => left.sortOrder - right.sortOrder);
  });

  const isDirty = computed(() => {
    if (!savedSnapshot.value) {
      return slug.value.trim().length > 0 || changeSummary.value.trim().length > 0;
    }

    return (
      slug.value !== savedSnapshot.value.slug ||
      changeSummary.value !== savedSnapshot.value.changeSummary ||
      !snapshotsEqual(data.value, savedSnapshot.value.data)
    );
  });

  const validationErrors = computed(() => {
    const merged: Record<string, string[]> = { ...clientErrors.value };
    for (const [key, messages] of Object.entries(serverErrors.value)) {
      merged[key] = [...(merged[key] ?? []), ...messages];
    }
    return merged;
  });

  function initializeForCreate(type: ContentType): void {
    slug.value = '';
    changeSummary.value = '';
    data.value = createEmptyEntryData(type.fields);
    concurrencyToken.value = 1;
    savedSnapshot.value = null;
    clearErrors();
  }

  function applyEntry(entry: ContentEntry, fields: ContentTypeField[]): void {
    slug.value = entry.slug;
    changeSummary.value = '';
    data.value = normalizeEntryData(fields, entry.draftData);
    concurrencyToken.value = entry.concurrencyToken;
    savedSnapshot.value = {
      slug: entry.slug,
      changeSummary: '',
      data: normalizeEntryData(fields, entry.draftData),
      concurrencyToken: entry.concurrencyToken,
    };
    clearErrors();
  }

  function markSaved(entry: ContentEntry, fields: ContentTypeField[]): void {
    concurrencyToken.value = entry.concurrencyToken;
    savedSnapshot.value = {
      slug: entry.slug,
      changeSummary: '',
      data: normalizeEntryData(fields, entry.draftData),
      concurrencyToken: entry.concurrencyToken,
    };
    slug.value = entry.slug;
    changeSummary.value = '';
    data.value = normalizeEntryData(fields, entry.draftData);
    clearErrors();
  }

  function clearErrors(): void {
    clientErrors.value = {};
    serverErrors.value = {};
  }

  function setServerErrors(errors: Record<string, string[]> | undefined): void {
    serverErrors.value = errors ?? {};
  }

  function runClientValidation(includeChangeSummary: boolean): boolean {
    if (!contentType.value) {
      return false;
    }

    clientErrors.value = validateEntryClientSide(sortedFields.value, data.value, {
      slug: slug.value,
      changeSummary: includeChangeSummary ? changeSummary.value : undefined,
    });
    return Object.keys(clientErrors.value).length === 0;
  }

  function fieldErrors(fieldName: string): string[] {
    return validationErrors.value[fieldName] ?? [];
  }

  function generalErrors(): string[] {
    const messages: string[] = [];
    for (const [key, values] of Object.entries(validationErrors.value)) {
      if (key === '') {
        messages.push(...values);
      }
    }
    return messages;
  }

  return {
    slug,
    changeSummary,
    data,
    concurrencyToken,
    savedSnapshot,
    sortedFields,
    isDirty,
    validationErrors,
    initializeForCreate,
    applyEntry,
    markSaved,
    clearErrors,
    setServerErrors,
    runClientValidation,
    fieldErrors,
    generalErrors,
  };
}
