<script setup lang="ts">
import AppButton from '@/components/common/AppButton.vue';
import { useRouter } from 'vue-router';

const router = useRouter();
</script>

<template>
  <section class="page-card page-card--center">
    <h2>Access denied</h2>
    <p class="page-card__lead">
      You are signed in, but your account does not include the permissions required for this page.
    </p>
    <p class="page-card__hint">
      If you believe this is an error, contact an administrator.
    </p>
    <AppButton variant="secondary" @click="router.push({ name: 'dashboard' })">
      Back to dashboard
    </AppButton>
  </section>
</template>
