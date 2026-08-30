import { storeToRefs } from 'pinia';
import { useUiStore } from '@/stores/uiStore';

export function useLoading() {
  const ui = useUiStore();
  const { isGlobalLoading } = storeToRefs(ui);

  return {
    isGlobalLoading,
    beginLoading: ui.beginLoading,
    endLoading: ui.endLoading,
  };
}
