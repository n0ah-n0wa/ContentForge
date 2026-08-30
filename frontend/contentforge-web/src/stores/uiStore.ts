import { defineStore } from 'pinia';
import { computed, ref } from 'vue';

export const useUiStore = defineStore('ui', () => {
  const pendingRequests = ref(0);

  const isGlobalLoading = computed(() => pendingRequests.value > 0);

  function beginLoading(): void {
    pendingRequests.value += 1;
  }

  function endLoading(): void {
    pendingRequests.value = Math.max(0, pendingRequests.value - 1);
  }

  return {
    pendingRequests,
    isGlobalLoading,
    beginLoading,
    endLoading,
  };
});
