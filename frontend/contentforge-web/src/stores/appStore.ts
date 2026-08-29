import { defineStore } from 'pinia';
import { computed, ref } from 'vue';

export const useAppStore = defineStore('app', () => {
  const appName = ref('ContentForge');

  const title = computed(() => `${appName.value} Admin`);

  return {
    appName,
    title,
  };
});
