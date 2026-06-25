<script setup lang="ts">
import { ref } from 'vue';
import { RouterView, RouterLink } from 'vue-router';
import { routes } from './router/index';

const isDark = ref(false);

function toggleTheme() {
  isDark.value = !isDark.value;
  document.documentElement.setAttribute('data-theme', isDark.value ? 'dark' : 'light');
}
</script>

<template>
  <div class="app">
    <header class="app-header">
      <div class="inner">
        <div class="header-left">
          <span class="logo">
            <svg width="18" height="18" viewBox="0 0 18 18" fill="none" aria-hidden="true">
              <rect x="1" y="1" width="7" height="7" rx="2" fill="currentColor" opacity=".95"/>
              <rect x="10" y="1" width="7" height="7" rx="2" fill="currentColor" opacity=".6"/>
              <rect x="1" y="10" width="7" height="7" rx="2" fill="currentColor" opacity=".6"/>
              <rect x="10" y="10" width="7" height="7" rx="2" fill="currentColor" opacity=".25"/>
            </svg>
            Stratus Fab Tracker
          </span>
          <nav class="nav" aria-label="Main navigation">
            <RouterLink
              v-for="route in routes"
              :key="route.path"
              :to="route.path"
              class="nav-link"
              active-class="nav-link--active"
            >
              {{ route.name }}
            </RouterLink>
          </nav>
        </div>
        <button class="btn btn--ghost icon-btn" @click="toggleTheme"
          :aria-label="isDark ? 'Switch to light mode' : 'Switch to dark mode'">
          <!-- sun -->
          <svg v-if="!isDark" width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
            <circle cx="8" cy="8" r="3.25" stroke="currentColor" stroke-width="1.4"/>
            <line x1="8" y1=".75" x2="8" y2="2.5"   stroke="currentColor" stroke-width="1.4" stroke-linecap="round"/>
            <line x1="8" y1="13.5" x2="8" y2="15.25" stroke="currentColor" stroke-width="1.4" stroke-linecap="round"/>
            <line x1=".75" y1="8" x2="2.5"  y2="8"   stroke="currentColor" stroke-width="1.4" stroke-linecap="round"/>
            <line x1="13.5" y1="8" x2="15.25" y2="8" stroke="currentColor" stroke-width="1.4" stroke-linecap="round"/>
            <line x1="2.87" y1="2.87" x2="4.05" y2="4.05" stroke="currentColor" stroke-width="1.4" stroke-linecap="round"/>
            <line x1="11.95" y1="11.95" x2="13.13" y2="13.13" stroke="currentColor" stroke-width="1.4" stroke-linecap="round"/>
            <line x1="13.13" y1="2.87" x2="11.95" y2="4.05" stroke="currentColor" stroke-width="1.4" stroke-linecap="round"/>
            <line x1="4.05" y1="11.95" x2="2.87" y2="13.13" stroke="currentColor" stroke-width="1.4" stroke-linecap="round"/>
          </svg>
          <!-- moon -->
          <svg v-else width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
            <path d="M13.5 9.5a5.5 5.5 0 0 1-7-7 5.5 5.5 0 1 0 7 7z"
              stroke="currentColor" stroke-width="1.4" stroke-linejoin="round"/>
          </svg>
        </button>
      </div>
    </header>

    <main class="app-main">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
/* ── Layout ── */
.app { min-height: 100vh; background: var(--color-bg-page); }

/* ── Header ── */
.app-header {
  background: var(--color-bg-surface);
  border-bottom: 1px solid var(--color-border);
  position: sticky;
  top: 0;
  z-index: 10;
}
.inner {
  max-width: 960px;
  margin: 0 auto;
  padding: 0 var(--space-6);
  height: 52px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.header-left {
  display: flex;
  align-items: center;
  gap: var(--space-6);
}
.logo {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  font-size: var(--text-base);
  font-weight: var(--weight-medium);
  color: var(--color-brand);
  white-space: nowrap;
}

/* ── Nav ── */
.nav {
  display: flex;
  align-items: center;
  gap: var(--space-1);
}
.nav-link {
  padding: var(--space-1) var(--space-3);
  border-radius: var(--radius-md);
  font-size: var(--text-sm);
  font-weight: var(--weight-medium);
  color: var(--color-text-secondary);
  text-decoration: none;
  transition: color 0.15s, background 0.15s;
}
.nav-link:hover {
  color: var(--color-text-primary);
  background: var(--color-bg-inset);
}
.nav-link--active {
  color: var(--color-brand);
  background: var(--color-bg-inset);
}

/* ── Theme toggle ── */
.icon-btn {
  width: 32px;
  height: 32px;
  padding: 0;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--color-text-secondary);
  border-radius: var(--radius-md);
}
.icon-btn:hover { color: var(--color-text-primary); background: var(--color-bg-inset); }

/* ── Main ── */
.app-main {
  max-width: 960px;
  margin: 0 auto;
  padding: var(--space-6);
  display: flex;
  flex-direction: column;
  gap: var(--space-5);
}
</style>
