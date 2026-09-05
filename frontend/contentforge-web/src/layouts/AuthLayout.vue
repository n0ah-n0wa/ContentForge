<template>
  <div class="auth-layout">
    <a href="#auth-main" class="skip-link">Skip to sign in</a>
    <section class="auth-layout__panel" aria-labelledby="auth-layout-title">
      <header class="auth-layout__header">
        <p class="auth-layout__eyebrow">ContentForge</p>
        <h1 id="auth-layout-title">Administrative CMS</h1>
        <p class="auth-layout__subtitle">Sign in to manage content, media, and users.</p>
      </header>
      <main id="auth-main">
        <RouterView />
      </main>
    </section>
  </div>
</template>
