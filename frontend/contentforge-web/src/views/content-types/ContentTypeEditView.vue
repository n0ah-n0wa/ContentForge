<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import AppSpinner from '@/components/common/AppSpinner.vue';
import ContentTypeFieldEditor from '@/components/content-types/ContentTypeFieldEditor.vue';
import ContentTypeFieldList from '@/components/content-types/ContentTypeFieldList.vue';
import ContentTypeMetadataForm from '@/components/content-types/ContentTypeMetadataForm.vue';
import DestructiveChangePanel from '@/components/content-types/DestructiveChangePanel.vue';
import {
  addContentTypeField,
  deactivateContentType,
  deleteContentType,
  getContentType,
  listContentTypes,
  removeContentTypeField,
  renameContentTypeField,
  updateContentType,
  updateContentTypeField,
} from '@/api/contentTypes';
import { ApiError, getValidationMessages, isValidationProblem } from '@/api/errors';
import { useApiErrorHandling } from '@/composables/useApiErrorHandling';
import { useContentTypePermissions } from '@/composables/useContentTypePermissions';
import {
  getConfirmationMessage,
  isConfirmationRequiredError,
  isDependentEntriesDeleteError,
} from '@/utils/destructiveChanges';
import type { ContentType, ContentTypeField, FieldConfiguration } from '@/types/contentTypes';
import { FieldType } from '@/types/contentTypes';

type PendingAction =
  | {
      kind: 'delete-type';
      confirmedSafeDeletion: boolean;
    }
  | {
      kind: 'remove-field';
      fieldName: string;
    }
  | {
      kind: 'rename-field';
      fieldName: string;
      newName: string;
    }
  | {
      kind: 'update-field';
      fieldName: string;
      displayName: string;
      sortOrder: number;
      configuration: FieldConfiguration;
    };

const route = useRoute();
const router = useRouter();
const { handleError } = useApiErrorHandling();
const { canUpdate, canDelete } = useContentTypePermissions();

const contentTypeId = computed(() => route.params.id as string);
const contentType = ref<ContentType | null>(null);
const relationTargets = ref<ContentType[]>([]);
const metadata = ref({
  name: '',
  displayName: '',
  slug: '',
  description: '',
});

const loading = ref(true);
const savingMetadata = ref(false);
const fieldPanel = ref<'none' | 'add' | 'edit'>('none');
const selectedFieldName = ref<string | null>(null);
const fieldErrorMessage = ref<string | null>(null);
const fieldBusyName = ref<string | null>(null);
const pageErrorMessage = ref<string | null>(null);
const pendingAction = ref<PendingAction | null>(null);
const pendingLoading = ref(false);
const pendingMessage = ref<string | null>(null);

const selectedField = computed<ContentTypeField | undefined>(() =>
  contentType.value?.fields.find((field) => field.name === selectedFieldName.value),
);

const nextSortOrder = computed(() => {
  if (!contentType.value?.fields.length) {
    return 1;
  }

  return Math.max(...contentType.value.fields.map((field) => field.sortOrder)) + 1;
});

async function loadPage(): Promise<void> {
  loading.value = true;
  pageErrorMessage.value = null;

  try {
    const [loaded, targets] = await Promise.all([
      getContentType(contentTypeId.value),
      listContentTypes({ page: 1, pageSize: 200, sortBy: 'name', sortDirection: 'asc' }),
    ]);

    contentType.value = loaded;
    relationTargets.value = targets.items;
    metadata.value = {
      name: loaded.name,
      displayName: loaded.displayName,
      slug: loaded.slug,
      description: loaded.description ?? '',
    };
  } catch (error) {
    pageErrorMessage.value = 'Unable to load content type.';
    handleError(error, 'Failed to load content type');
  } finally {
    loading.value = false;
  }
}

async function saveMetadata(): Promise<void> {
  if (!contentType.value || !canUpdate.value) {
    return;
  }

  savingMetadata.value = true;
  pageErrorMessage.value = null;

  try {
    contentType.value = await updateContentType(contentType.value.id, {
      displayName: metadata.value.displayName.trim(),
      slug: metadata.value.slug.trim(),
      description: metadata.value.description.trim() || null,
    });
  } catch (error) {
    pageErrorMessage.value =
      error instanceof ApiError && error.isValidationError && isValidationProblem(error.problem)
        ? getValidationMessages(error.problem).join(' ')
        : 'Unable to save content type metadata.';
    handleError(error, 'Update content type failed');
  } finally {
    savingMetadata.value = false;
  }
}

async function deactivate(): Promise<void> {
  if (!contentType.value || !canUpdate.value) {
    return;
  }

  contentType.value = await deactivateContentType(contentType.value.id);
}

async function requestDeleteType(): Promise<void> {
  pendingAction.value = { kind: 'delete-type', confirmedSafeDeletion: false };
  pendingMessage.value =
    'Deleting a content type removes its schema. Confirm to proceed.';
}

async function executeDeleteType(confirmedSafeDeletion: boolean): Promise<void> {
  if (!contentType.value) {
    return;
  }

  try {
    await deleteContentType(contentType.value.id, confirmedSafeDeletion);
    await router.push({ name: 'content-types' });
    pendingAction.value = null;
  } catch (error) {
    if (isDependentEntriesDeleteError(error) && !confirmedSafeDeletion) {
      pendingMessage.value = getConfirmationMessage(error);
      pendingAction.value = { kind: 'delete-type', confirmedSafeDeletion: true };
      return;
    }

    pageErrorMessage.value = getConfirmationMessage(error);
    handleError(error, 'Delete content type failed');
    pendingAction.value = null;
  }
}

function openAddField(): void {
  fieldPanel.value = 'add';
  selectedFieldName.value = null;
  fieldErrorMessage.value = null;
}

function openEditField(fieldName: string): void {
  selectedFieldName.value = fieldName;
  fieldPanel.value = 'edit';
  fieldErrorMessage.value = null;
}

async function handleAddField(payload: {
  name: string;
  fieldType: FieldType;
  displayName: string;
  sortOrder: number;
  configuration: FieldConfiguration;
}): Promise<void> {
  if (!contentType.value) {
    return;
  }

  fieldBusyName.value = payload.name;
  fieldErrorMessage.value = null;

  try {
    contentType.value = await addContentTypeField(contentType.value.id, payload);
    fieldPanel.value = 'none';
  } catch (error) {
    fieldErrorMessage.value =
      error instanceof ApiError && error.isValidationError && isValidationProblem(error.problem)
        ? getValidationMessages(error.problem).join(' ')
        : getConfirmationMessage(error);
    handleError(error, 'Add field failed');
  } finally {
    fieldBusyName.value = null;
  }
}

async function handleUpdateField(
  payload: {
    displayName: string;
    sortOrder: number;
    configuration: FieldConfiguration;
  },
  confirmedDestructiveChange = false,
): Promise<void> {
  if (!contentType.value || !selectedField.value) {
    return;
  }

  fieldBusyName.value = selectedField.value.name;
  fieldErrorMessage.value = null;

  try {
    contentType.value = await updateContentTypeField(contentType.value.id, selectedField.value.name, {
      ...payload,
      confirmedDestructiveChange,
    });
    fieldPanel.value = 'none';
    pendingAction.value = null;
  } catch (error) {
    if (isConfirmationRequiredError(error) && !confirmedDestructiveChange) {
      pendingAction.value = {
        kind: 'update-field',
        fieldName: selectedField.value.name,
        displayName: payload.displayName,
        sortOrder: payload.sortOrder,
        configuration: payload.configuration,
      };
      pendingMessage.value = getConfirmationMessage(error);
      return;
    }

    fieldErrorMessage.value =
      error instanceof ApiError && error.isValidationError && isValidationProblem(error.problem)
        ? getValidationMessages(error.problem).join(' ')
        : getConfirmationMessage(error);
    handleError(error, 'Update field failed');
  } finally {
    fieldBusyName.value = null;
  }
}

function requestRemoveField(fieldName: string): void {
  pendingAction.value = { kind: 'remove-field', fieldName };
  pendingMessage.value = `Removing field "${fieldName}" may leave legacy values in existing entries.`;
}

async function executeRemoveField(fieldName: string, confirmed: boolean): Promise<void> {
  if (!contentType.value) {
    return;
  }

  fieldBusyName.value = fieldName;
  try {
    contentType.value = await removeContentTypeField(contentType.value.id, fieldName, confirmed);
    if (selectedFieldName.value === fieldName) {
      selectedFieldName.value = null;
      fieldPanel.value = 'none';
    }
    pendingAction.value = null;
  } catch (error) {
    if (isConfirmationRequiredError(error) && !confirmed) {
      pendingMessage.value = getConfirmationMessage(error);
      pendingAction.value = { kind: 'remove-field', fieldName };
      return;
    }

    pageErrorMessage.value = getConfirmationMessage(error);
    handleError(error, 'Remove field failed');
  } finally {
    fieldBusyName.value = null;
  }
}

function requestRenameField(fieldName: string): void {
  const newName = window.prompt('Enter the new field name:', fieldName);
  if (!newName || newName.trim() === fieldName) {
    return;
  }

  pendingAction.value = { kind: 'rename-field', fieldName, newName: newName.trim() };
  pendingMessage.value = `Renaming "${fieldName}" to "${newName.trim()}" requires confirmation.`;
}

async function executeRenameField(
  fieldName: string,
  newName: string,
  confirmed: boolean,
): Promise<void> {
  if (!contentType.value) {
    return;
  }

  fieldBusyName.value = fieldName;
  try {
    contentType.value = await renameContentTypeField(
      contentType.value.id,
      fieldName,
      newName,
      confirmed,
    );
    pendingAction.value = null;
  } catch (error) {
    if (isConfirmationRequiredError(error) && !confirmed) {
      pendingMessage.value = getConfirmationMessage(error);
      pendingAction.value = { kind: 'rename-field', fieldName, newName };
      return;
    }

    pageErrorMessage.value = getConfirmationMessage(error);
    handleError(error, 'Rename field failed');
  } finally {
    fieldBusyName.value = null;
  }
}

async function reorderField(fieldName: string, direction: 'up' | 'down'): Promise<void> {
  if (!contentType.value) {
    return;
  }

  const ordered = [...contentType.value.fields].sort((a, b) => a.sortOrder - b.sortOrder);
  const index = ordered.findIndex((field) => field.name === fieldName);
  const swapIndex = direction === 'up' ? index - 1 : index + 1;
  if (index < 0 || swapIndex < 0 || swapIndex >= ordered.length) {
    return;
  }

  const current = ordered[index];
  const target = ordered[swapIndex];
  fieldBusyName.value = fieldName;

  try {
    await updateContentTypeField(contentType.value.id, current.name, {
      displayName: current.displayName,
      sortOrder: target.sortOrder,
      configuration: current.configuration,
      confirmedDestructiveChange: false,
    });
    contentType.value = await updateContentTypeField(contentType.value.id, target.name, {
      displayName: target.displayName,
      sortOrder: current.sortOrder,
      configuration: target.configuration,
      confirmedDestructiveChange: false,
    });
    contentType.value = await getContentType(contentType.value.id);
  } catch (error) {
    handleError(error, 'Reorder field failed');
  } finally {
    fieldBusyName.value = null;
  }
}

async function confirmPendingAction(): Promise<void> {
  if (!pendingAction.value) {
    return;
  }

  pendingLoading.value = true;
  const action = pendingAction.value;

  try {
    switch (action.kind) {
      case 'delete-type':
        await executeDeleteType(action.confirmedSafeDeletion);
        break;
      case 'remove-field':
        await executeRemoveField(action.fieldName, true);
        break;
      case 'rename-field':
        await executeRenameField(action.fieldName, action.newName, true);
        break;
      case 'update-field':
        await handleUpdateField(
          {
            displayName: action.displayName,
            sortOrder: action.sortOrder,
            configuration: action.configuration,
          },
          true,
        );
        break;
    }
  } finally {
    pendingLoading.value = false;
  }
}

function cancelPendingAction(): void {
  pendingAction.value = null;
  pendingMessage.value = null;
}

onMounted(() => {
  void loadPage();
});
</script>

<template>
  <section class="page-card">
    <header class="page-header">
      <div>
        <h2>{{ contentType?.displayName ?? 'Content type' }}</h2>
        <p v-if="contentType" class="page-card__lead">
          Manage schema metadata, field definitions, and safe schema evolution rules.
        </p>
      </div>
      <AppButton variant="secondary" @click="router.push({ name: 'content-types' })">
        Back to list
      </AppButton>
    </header>

    <div v-if="loading" class="inline-loading">
      <AppSpinner label="Loading content type" />
      <span>Loading content type…</span>
    </div>

    <template v-else-if="contentType">
      <AppAlert
        v-if="pageErrorMessage"
        kind="error"
        title="Operation failed"
        :message="pageErrorMessage"
      />

      <AppAlert
        v-if="!contentType.isActive"
        kind="warning"
        title="Inactive content type"
        message="This content type is inactive and cannot be used for new content entries."
      />

      <DestructiveChangePanel
        v-if="pendingAction && pendingMessage"
        :title="'Confirm schema change'"
        :message="pendingMessage"
        :loading="pendingLoading"
        @confirm="confirmPendingAction"
        @cancel="cancelPendingAction"
      />

      <section class="editor-section">
        <header class="section-header">
          <div>
            <h3>Metadata</h3>
            <p>System name <code>{{ contentType.name }}</code> · Version {{ contentType.version }}</p>
          </div>
        </header>

        <form class="stack-form" @submit.prevent="saveMetadata">
          <ContentTypeMetadataForm
            v-model="metadata"
            mode="edit"
            :disabled="!canUpdate || savingMetadata"
          />
          <div class="form-actions">
            <AppButton
              v-if="canUpdate && contentType.isActive"
              variant="secondary"
              type="button"
              @click="deactivate"
            >
              Deactivate
            </AppButton>
            <AppButton
              v-if="canDelete"
              variant="secondary"
              type="button"
              @click="requestDeleteType"
            >
              Delete
            </AppButton>
            <AppButton type="submit" :loading="savingMetadata" :disabled="!canUpdate">
              Save metadata
            </AppButton>
          </div>
        </form>
      </section>

      <div class="editor-grid">
        <ContentTypeFieldList
          :fields="contentType.fields"
          :selected-field-name="selectedFieldName"
          :can-update="canUpdate"
          :busy-field-name="fieldBusyName"
          @select="openEditField"
          @move-up="reorderField($event, 'up')"
          @move-down="reorderField($event, 'down')"
          @remove="requestRemoveField"
          @rename="requestRenameField"
        />

        <div class="editor-panel">
          <div v-if="canUpdate && fieldPanel === 'none'" class="editor-panel__empty">
            <p>Select a field to edit or add a new field definition.</p>
            <AppButton @click="openAddField">Add field</AppButton>
          </div>

          <ContentTypeFieldEditor
            v-else-if="canUpdate && fieldPanel === 'add'"
            mode="add"
            :content-type="contentType"
            :relation-targets="relationTargets"
            :next-sort-order="nextSortOrder"
            :loading="fieldBusyName !== null"
            :error-message="fieldErrorMessage"
            @submit="handleAddField"
            @cancel="fieldPanel = 'none'"
          />

          <ContentTypeFieldEditor
            v-else-if="canUpdate && fieldPanel === 'edit' && selectedField"
            mode="edit"
            :content-type="contentType"
            :relation-targets="relationTargets"
            :field="selectedField"
            :next-sort-order="nextSortOrder"
            :loading="fieldBusyName !== null"
            :error-message="fieldErrorMessage"
            @submit="
              handleUpdateField({
                displayName: $event.displayName,
                sortOrder: $event.sortOrder,
                configuration: $event.configuration,
              })
            "
            @cancel="fieldPanel = 'none'"
          />

          <AppAlert
            v-else-if="!canUpdate"
            kind="warning"
            title="Read-only"
            message="You need the contentType.update permission to modify fields."
          />
        </div>
      </div>
    </template>
  </section>
</template>
