<script setup lang="ts">
import { computed, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import { useApiErrorHandling } from '@/composables/useApiErrorHandling';
import { describeAuthError, useAuthStore } from '@/stores/authStore';

const authStore = useAuthStore();
const router = useRouter();
const route = useRoute();
const { handleError } = useApiErrorHandling();

const form = reactive({
  email: '',
  password: '',
});

const errorMessage = ref<string | null>(null);

const formAlertId = 'login-form-alert';

const sessionNotice = computed(() => {
  if (route.query.reason === 'expired') {
    return 'Your session expired. Sign in again to continue.';
  }

  if (route.query.reason === 'invalid') {
    return 'Your session is no longer valid. Sign in again to continue.';
  }

  return null;
});

async function onSubmit(): Promise<void> {
  errorMessage.value = null;

  try {
    await authStore.login(form.email.trim(), form.password);
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/dashboard';
    await router.replace(redirect);
  } catch (error) {
    errorMessage.value = describeAuthError(error);
    handleError(error, 'Sign in failed');
  } finally {
    form.password = '';
  }
}
</script>

<template>
  <form class="login-form" :aria-describedby="formAlertId" @submit.prevent="onSubmit">
    <div :id="formAlertId">
      <AppAlert
        v-if="sessionNotice"
        kind="warning"
        title="Session ended"
        :message="sessionNotice"
      />

      <AppAlert v-if="errorMessage" kind="error" title="Sign in failed" :message="errorMessage" />
    </div>

    <label class="form-field">
      <span>Email</span>
      <input v-model="form.email" type="email" name="email" autocomplete="username" required />
    </label>

    <label class="form-field">
      <span>Password</span>
      <input
        v-model="form.password"
        type="password"
        name="password"
        autocomplete="current-password"
        required
      />
    </label>

    <AppButton type="submit" :loading="authStore.isAuthenticating"> Sign in </AppButton>
  </form>
</template>
