declare module 'vue-router' {
  interface RouteMeta {
    title?: string;
    requiresAuth?: boolean;
    guestOnly?: boolean;
    publicPreview?: boolean;
    permissions?: string[];
  }
}
