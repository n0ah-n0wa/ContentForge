import '@/types/router';
import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from '@/App.vue';
import router from '@/router';
import { configureApiAuthSession } from '@/api/authSession';
import { setAccessTokenProvider, setRefreshHandler } from '@/api/client';
import { configureApiLoading } from '@/api/loading';
import { useAuthStore } from '@/stores/authStore';
import { useUiStore } from '@/stores/uiStore';
import '@/assets/main.css';

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
app.use(router);

const authStore = useAuthStore(pinia);
const uiStore = useUiStore(pinia);

setAccessTokenProvider(() => authStore.getAccessToken());
setRefreshHandler(() => authStore.refreshSession());
configureApiLoading({
  beginLoading: () => uiStore.beginLoading(),
  endLoading: () => uiStore.endLoading(),
});
configureApiAuthSession({
  ensureSession: () => authStore.ensureSession(),
  onUnauthorized: (reason) => {
    authStore.clearSession('expired');
    void router.push({
      name: 'login',
      query: { reason },
    });
  },
});

app.mount('#app');
