<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import AppSpinner from '@/components/common/AppSpinner.vue';
import ConfirmActionPanel from '@/components/common/ConfirmActionPanel.vue';
import RolePermissionsPanel from '@/components/users/RolePermissionsPanel.vue';
import UserFormFields from '@/components/users/UserFormFields.vue';
import { disableUser, enableUser, getUser, updateUser } from '@/api/users';
import { listRoles } from '@/api/roles';
import { ApiError, getValidationMessages, isValidationProblem } from '@/api/errors';
import { useApiErrorHandling } from '@/composables/useApiErrorHandling';
import { useUserPermissions } from '@/composables/useUserPermissions';
import { useAuthStore } from '@/stores/authStore';
import {
  formatUserTimestamp,
  getUserRoleLabel,
  type RoleDefinition,
  type UserAccount,
  type UserRoleName,
} from '@/types/users';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const { handleError } = useApiErrorHandling();
const { canRead, canUpdate, canDisable } = useUserPermissions();

const user = ref<UserAccount | null>(null);
const roles = ref<RoleDefinition[]>([]);
const displayName = ref('');
const role = ref<UserRoleName>('Viewer');
const loading = ref(true);
const saving = ref(false);
const togglingStatus = ref(false);
const errorMessage = ref<string | null>(null);
const showDisableConfirm = ref(false);
const showEnableConfirm = ref(false);

const userId = computed(() => String(route.params.id ?? ''));
const isCurrentUser = computed(() => user.value?.id === authStore.user?.id);
const canToggleStatus = computed(() => {
  if (!user.value || isCurrentUser.value) {
    return false;
  }

  return user.value.isActive ? canDisable.value : canUpdate.value;
});

async function loadUser(): Promise<void> {
  if (!canRead.value) {
    loading.value = false;
    return;
  }

  loading.value = true;
  errorMessage.value = null;

  try {
    const [loadedUser, loadedRoles] = await Promise.all([
      getUser(userId.value),
      listRoles(),
    ]);
    user.value = loadedUser;
    roles.value = loadedRoles;
    displayName.value = loadedUser.displayName;
    role.value = loadedUser.role;
  } catch (error) {
    errorMessage.value = 'Unable to load user details.';
    handleError(error, 'Failed to load user');
  } finally {
    loading.value = false;
  }
}

async function saveChanges(): Promise<void> {
  if (!user.value || !canUpdate.value) {
    return;
  }

  saving.value = true;
  errorMessage.value = null;

  try {
    user.value = await updateUser(user.value.id, {
      displayName: displayName.value.trim(),
      role: role.value,
    });
    displayName.value = user.value.displayName;
    role.value = user.value.role;
  } catch (error) {
    if (error instanceof ApiError && error.isValidationError && isValidationProblem(error.problem)) {
      errorMessage.value = getValidationMessages(error.problem).join(' ');
    } else if (error instanceof ApiError && error.isForbidden) {
      errorMessage.value = 'You do not have permission to update this user.';
    } else if (error instanceof Error) {
      errorMessage.value = error.message;
    } else {
      errorMessage.value = 'Unable to save user changes.';
    }
    handleError(error, 'Update user failed');
  } finally {
    saving.value = false;
  }
}

async function confirmDisable(): Promise<void> {
  if (!user.value || !canDisable.value) {
    return;
  }

  togglingStatus.value = true;
  try {
    user.value = await disableUser(user.value.id);
    showDisableConfirm.value = false;
  } catch (error) {
    handleError(error, 'Failed to disable user');
  } finally {
    togglingStatus.value = false;
  }
}

async function confirmEnable(): Promise<void> {
  if (!user.value || !canUpdate.value) {
    return;
  }

  togglingStatus.value = true;
  try {
    user.value = await enableUser(user.value.id);
    showEnableConfirm.value = false;
  } catch (error) {
    handleError(error, 'Failed to enable user');
  } finally {
    togglingStatus.value = false;
  }
}

onMounted(() => {
  void loadUser();
});
</script>

<template>
  <section class="page-card">
    <header class="page-header">
      <div>
        <h2>Manage user</h2>
        <p v-if="user" class="page-card__lead">
          {{ user.email }} · {{ getUserRoleLabel(user.role) }} ·
          {{ user.isActive ? 'Active' : 'Disabled' }}
        </p>
      </div>
      <AppButton variant="secondary" @click="router.push({ name: 'users' })">
        Back to users
      </AppButton>
    </header>

    <AppAlert
      v-if="!canRead"
      kind="warning"
      title="Access restricted"
      message="You do not have permission to view users."
    />

    <div v-else-if="loading" class="inline-loading">
      <AppSpinner label="Loading user" />
      <span>Loading user…</span>
    </div>

    <template v-else-if="user">
      <AppAlert
        v-if="!user.isActive"
        kind="warning"
        title="Account disabled"
        message="This user cannot sign in until the account is reactivated."
      />

      <AppAlert
        v-if="errorMessage"
        kind="error"
        title="Action failed"
        :message="errorMessage"
      />

      <div class="user-detail-grid">
        <section class="panel-card">
          <h3>Account details</h3>
          <dl class="metadata-list">
            <div>
              <dt>Email</dt>
              <dd>{{ user.email }}</dd>
            </div>
            <div>
              <dt>Created</dt>
              <dd>{{ formatUserTimestamp(user.createdAt) }}</dd>
            </div>
            <div>
              <dt>Last login</dt>
              <dd>{{ formatUserTimestamp(user.lastLoginAt) }}</dd>
            </div>
            <div>
              <dt>Status</dt>
              <dd>
                <span class="status-badge" :class="{ 'status-badge--inactive': !user.isActive }">
                  {{ user.isActive ? 'Active' : 'Disabled' }}
                </span>
              </dd>
            </div>
          </dl>

          <form class="stack-form" @submit.prevent="saveChanges">
            <UserFormFields
              v-model:display-name="displayName"
              v-model:role="role"
              mode="edit"
              :disabled="!canUpdate || saving || togglingStatus"
            />

            <div class="form-actions">
              <AppButton type="submit" :loading="saving" :disabled="!canUpdate">
                Save changes
              </AppButton>
              <AppButton
                v-if="user.isActive && canToggleStatus"
                variant="secondary"
                type="button"
                :loading="togglingStatus"
                @click="showDisableConfirm = true"
              >
                Disable user
              </AppButton>
              <AppButton
                v-else-if="!user.isActive && canToggleStatus"
                variant="secondary"
                type="button"
                :loading="togglingStatus"
                @click="showEnableConfirm = true"
              >
                Activate user
              </AppButton>
            </div>

            <p v-if="isCurrentUser" class="entry-field__hint">
              You cannot disable your own account.
            </p>
          </form>
        </section>

        <section class="panel-card">
          <h3>Role permissions</h3>
          <p class="page-card__lead">
            Permissions granted by the selected role. Effective access is always verified by the backend.
          </p>
          <RolePermissionsPanel :roles="roles" :selected-role="role" />
        </section>
      </div>

      <ConfirmActionPanel
        v-if="showDisableConfirm"
        title="Disable user"
        :message="`Disable ${user.email}? The user will be signed out and cannot sign in until reactivated.`"
        confirm-label="Disable user"
        :loading="togglingStatus"
        @confirm="confirmDisable"
        @cancel="showDisableConfirm = false"
      />

      <ConfirmActionPanel
        v-if="showEnableConfirm"
        title="Activate user"
        :message="`Activate ${user.email}? The user will be able to sign in again.`"
        confirm-label="Activate user"
        :loading="togglingStatus"
        @confirm="confirmEnable"
        @cancel="showEnableConfirm = false"
      />
    </template>
  </section>
</template>
