<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import UserFormFields from '@/components/users/UserFormFields.vue';
import { createUser } from '@/api/users';
import { ApiError, getValidationMessages, isValidationProblem } from '@/api/errors';
import { useApiErrorHandling } from '@/composables/useApiErrorHandling';
import { useUserPermissions } from '@/composables/useUserPermissions';
import type { UserRoleName } from '@/types/users';

const router = useRouter();
const { handleError } = useApiErrorHandling();
const { canCreate } = useUserPermissions();

const email = ref('');
const displayName = ref('');
const password = ref('');
const role = ref<UserRoleName>('Viewer');
const saving = ref(false);
const errorMessage = ref<string | null>(null);

async function onSubmit(): Promise<void> {
  if (!canCreate.value) {
    return;
  }

  saving.value = true;
  errorMessage.value = null;

  try {
    const created = await createUser({
      email: email.value.trim(),
      displayName: displayName.value.trim(),
      password: password.value,
      role: role.value,
    });
    await router.push({ name: 'user-edit', params: { id: created.id } });
  } catch (error) {
    if (error instanceof ApiError && error.isValidationError && isValidationProblem(error.problem)) {
      errorMessage.value = getValidationMessages(error.problem).join(' ');
    } else if (error instanceof ApiError && error.isForbidden) {
      errorMessage.value = 'You do not have permission to create users.';
    } else if (error instanceof Error) {
      errorMessage.value = error.message;
    } else {
      errorMessage.value = 'Unable to create user.';
    }
    handleError(error, 'Create user failed');
  } finally {
    saving.value = false;
    password.value = '';
  }
}
</script>

<template>
  <section class="page-card">
    <header class="page-header">
      <div>
        <h2>Create user</h2>
        <p class="page-card__lead">
          Add a new CMS account with an initial role. The backend validates credentials and permissions.
        </p>
      </div>
    </header>

    <AppAlert
      v-if="!canCreate"
      kind="warning"
      title="Insufficient permissions"
      message="You need the user.create permission to create users."
    />

    <AppAlert
      v-if="errorMessage"
      kind="error"
      title="Create failed"
      :message="errorMessage"
    />

    <form class="stack-form" @submit.prevent="onSubmit">
      <UserFormFields
        v-model:email="email"
        v-model:display-name="displayName"
        v-model:password="password"
        v-model:role="role"
        mode="create"
        :disabled="!canCreate || saving"
      />

      <div class="form-actions">
        <AppButton variant="secondary" type="button" @click="router.push({ name: 'users' })">
          Cancel
        </AppButton>
        <AppButton type="submit" :loading="saving" :disabled="!canCreate">
          Create user
        </AppButton>
      </div>
    </form>
  </section>
</template>
