<script setup lang="ts">
import { onMounted, ref, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';

type BomItem = { partNumber: string; quantity: number };
type StatusEvent = { station: number; changedAt: string; changedBy: string };
type Spool = {
  id: string;
  packageId: string;
  spoolNumber: string;
  dueDate: string;
  bom: BomItem[];
  statusHistory: StatusEvent[];
};

const STATIONS = [
  { name: 'Detailing', key: 'detailing', icon: 'ti-pencil' },
  { name: 'Cut',       key: 'cut',       icon: 'ti-scissors' },
  { name: 'Weld',      key: 'weld',      icon: 'ti-flame' },
  { name: 'QC',        key: 'qc',        icon: 'ti-shield-check' },
  { name: 'Shipped',   key: 'shipped',   icon: 'ti-truck' },
  { name: 'Installed', key: 'installed', icon: 'ti-building-factory' },
];

const STATION_NAMES = STATIONS.map(s => s.name);

const route = useRoute();
const router = useRouter();

const spool = ref<Spool | null>(null);
const error = ref('');
const advancing = ref(false);
const advanceError = ref('');

const currentStationIndex = computed(() => {
  if (!spool.value) return 0;
  if (!spool.value.statusHistory.length) return 0;
  const latest = spool.value.statusHistory.reduce((a, b) =>
    new Date(a.changedAt) > new Date(b.changedAt) ? a : b
  );
  return latest.station;
});

const nextStationName = computed(() =>
  currentStationIndex.value < STATIONS.length - 1
    ? STATIONS[currentStationIndex.value + 1].name
    : null
);

const isOverdue = computed(() => {
  if (!spool.value) return false;
  return new Date(spool.value.dueDate) < new Date(new Date().toDateString());
});

const sortedHistory = computed(() => {
  if (!spool.value) return [];
  return [...spool.value.statusHistory].sort(
    (a, b) => new Date(b.changedAt).getTime() - new Date(a.changedAt).getTime()
  );
});

function stationNodeState(index: number): 'done' | 'current' | 'upcoming' {
  if (index < currentStationIndex.value) return 'done';
  if (index === currentStationIndex.value) return 'current';
  return 'upcoming';
}

function connectorState(index: number): 'done' | 'upcoming' {
  return index < currentStationIndex.value ? 'done' : 'upcoming';
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
    hour: 'numeric', minute: '2-digit',
  });
}

function stationEnteredDate(stationIndex: number): string {
  if (!spool.value) return '—';
  const event = spool.value.statusHistory.find(e => e.station === stationIndex);
  if (!event) return '—';
  return new Date(event.changedAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}

async function load() {
  try {
    const res = await fetch(`/api/spools/${route.params.id}`);
    if (res.status === 404) { error.value = 'Spool not found.'; return; }
    if (!res.ok) throw new Error();
    spool.value = await res.json();
  } catch {
    error.value = 'Unable to load spool. Make sure the API is running.';
  }
}

async function advance() {
  if (!spool.value || advancing.value) return;
  advancing.value = true;
  advanceError.value = '';
  try {
    const res = await fetch(`/api/spools/${spool.value.id}/advance`, { method: 'POST' });
    if (res.status === 400) { advanceError.value = 'This spool cannot be advanced.'; return; }
    if (res.status === 404) { advanceError.value = 'Spool not found.'; return; }
    if (!res.ok) throw new Error();
    await load();
  } catch {
    advanceError.value = 'Failed to advance spool. Please try again.';
  } finally {
    advancing.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div>
    <RouterLink to="/spools" class="back-link">
      <svg width="14" height="14" viewBox="0 0 14 14" fill="none" aria-hidden="true">
        <path d="M8.5 2.5L4 7l4.5 4.5" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round"/>
      </svg>
      Spools
    </RouterLink>

    <div v-if="error" class="alert" role="alert">
      <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true" style="flex-shrink:0">
        <circle cx="8" cy="8" r="7" stroke="currentColor" stroke-width="1.4"/>
        <line x1="8" y1="5" x2="8" y2="8.5" stroke="currentColor" stroke-width="1.4" stroke-linecap="round"/>
        <circle cx="8" cy="11" r=".75" fill="currentColor"/>
      </svg>
      {{ error }}
    </div>

    <template v-if="spool">
      <!-- ── Header ── -->
      <div class="page-header">
        <div class="header-left">
          <div class="spool-avatar" aria-hidden="true">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
              <circle cx="10" cy="10" r="8" stroke="currentColor" stroke-width="1.6"/>
              <circle cx="10" cy="10" r="3" fill="currentColor" opacity=".5"/>
              <line x1="10" y1="2" x2="10" y2="5" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/>
              <line x1="10" y1="15" x2="10" y2="18" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/>
              <line x1="2" y1="10" x2="5" y2="10" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/>
              <line x1="15" y1="10" x2="18" y2="10" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/>
            </svg>
          </div>
          <div>
            <div class="title-row">
              <h1 class="spool-title">{{ spool.spoolNumber }}</h1>
              <span class="badge" :class="`station--${STATIONS[currentStationIndex].key}`">
                <span class="badge__dot" aria-hidden="true"></span>
                {{ STATIONS[currentStationIndex].name }}
              </span>
              <span v-if="isOverdue" class="badge badge--overdue">
                <svg width="11" height="11" viewBox="0 0 12 12" fill="none" aria-hidden="true">
                  <path d="M6 1L1 10h10L6 1z" stroke="currentColor" stroke-width="1.3" stroke-linejoin="round"/>
                  <line x1="6" y1="5" x2="6" y2="7.5" stroke="currentColor" stroke-width="1.2" stroke-linecap="round"/>
                  <circle cx="6" cy="9" r=".6" fill="currentColor"/>
                </svg>
                Past due
              </span>
            </div>
            <p class="spool-sub">Package {{ spool.packageId }} &middot; {{ spool.id }}</p>
          </div>
        </div>

        <button
          v-if="nextStationName"
          class="btn btn--primary"
          :disabled="advancing"
          @click="advance"
          :aria-label="`Advance spool to ${nextStationName}`"
        >
          <svg width="15" height="15" viewBox="0 0 15 15" fill="none" aria-hidden="true">
            <circle cx="7.5" cy="7.5" r="6.5" stroke="currentColor" stroke-width="1.3"/>
            <path d="M5.5 7.5h4m-2-2 2 2-2 2" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          {{ advancing ? 'Advancing…' : `Advance to ${nextStationName}` }}
        </button>
        <span v-else class="terminal-badge">
          <svg width="13" height="13" viewBox="0 0 13 13" fill="none" aria-hidden="true">
            <path d="M2 7l3 3 6-6" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          Fully installed
        </span>
      </div>

      <div v-if="advanceError" class="alert" role="alert" style="margin-bottom:var(--space-4)">
        {{ advanceError }}
      </div>

      <!-- ── Station flow ── -->
      <section class="card flow-card" aria-label="Station flow">
        <div class="flow-scroll">
          <div class="flow" role="list">
            <template v-for="(station, i) in STATIONS" :key="station.name">
              <div class="station-node" role="listitem" :aria-current="i === currentStationIndex ? 'step' : undefined">
                <div class="node-circle" :class="stationNodeState(i)">
                  <svg v-if="stationNodeState(i) === 'done'" width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
                    <path d="M3.5 8.5l3 3 6-6" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/>
                  </svg>
                  <span v-else class="node-index" aria-hidden="true">{{ i + 1 }}</span>
                </div>
                <span class="node-label" :class="stationNodeState(i)">{{ station.name }}</span>
                <span class="node-date">{{ stationEnteredDate(i) }}</span>
                <span v-if="i === currentStationIndex" class="current-pill">current</span>
              </div>
              <div
                v-if="i < STATIONS.length - 1"
                class="connector"
                :class="connectorState(i)"
                aria-hidden="true"
              ></div>
            </template>
          </div>
        </div>
      </section>

      <!-- ── Details + BOM ── -->
      <div class="two-col">
        <section class="card" aria-labelledby="details-heading">
          <h2 id="details-heading" class="section-title">Spool details</h2>
          <dl class="details-list">
            <div class="detail-row">
              <dt>Spool number</dt><dd>{{ spool.spoolNumber }}</dd>
            </div>
            <div class="detail-row">
              <dt>Package</dt><dd>{{ spool.packageId }}</dd>
            </div>
            <div class="detail-row">
              <dt>ID</dt><dd class="mono">{{ spool.id }}</dd>
            </div>
            <div class="detail-row">
              <dt>Due date</dt>
              <dd :class="isOverdue ? 'overdue' : ''">{{ formatDate(spool.dueDate) }}</dd>
            </div>
            <div class="detail-row">
              <dt>Current station</dt>
              <dd>
                <span class="badge" :class="`station--${STATIONS[currentStationIndex].key}`">
                  <span class="badge__dot" aria-hidden="true"></span>
                  {{ STATIONS[currentStationIndex].name }}
                </span>
              </dd>
            </div>
          </dl>
        </section>

        <section class="card" aria-labelledby="bom-heading">
          <h2 id="bom-heading" class="section-title">Bill of materials</h2>
          <p v-if="!spool.bom.length" class="empty-state">No items on BOM.</p>
          <ul v-else class="bom-list" role="list">
            <li v-for="(item, i) in spool.bom" :key="i" class="bom-row">
              <span class="bom-part">{{ item.partNumber }}</span>
              <span class="bom-qty">&times; {{ item.quantity }}</span>
            </li>
          </ul>
        </section>
      </div>

      <!-- ── History ── -->
      <section class="card" aria-labelledby="history-heading">
        <h2 id="history-heading" class="section-title">Station history</h2>
        <p v-if="!sortedHistory.length" class="empty-state">No history recorded.</p>
        <ol v-else class="history-list" role="list">
          <li v-for="event in sortedHistory" :key="event.changedAt" class="history-row">
            <span class="history-dot" :class="`station-dot--${STATIONS[event.station]?.key}`" aria-hidden="true"></span>
            <div class="history-body">
              <span class="history-station">{{ STATION_NAMES[event.station] ?? `Station ${event.station}` }}</span>
              <span class="history-meta">{{ formatDateTime(event.changedAt) }} &middot; {{ event.changedBy }}</span>
            </div>
          </li>
        </ol>
      </section>
    </template>

    <p v-else-if="!error" class="loading" aria-live="polite">Loading…</p>
  </div>
</template>

<style scoped>
.back-link {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
  text-decoration: none;
  margin-bottom: var(--space-5);
  transition: color var(--transition-fast);
}
.back-link:hover { color: var(--color-text-primary); }

/* ── Alert ── */
.alert {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-3) var(--space-4);
  border-radius: var(--radius-md);
  font-size: var(--text-sm);
  background: var(--color-danger-bg);
  color: var(--color-danger);
  border: 1px solid var(--color-danger);
  margin-bottom: var(--space-5);
}

/* ── Page header ── */
.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-4);
  margin-bottom: var(--space-5);
  flex-wrap: wrap;
}
.header-left { display: flex; align-items: flex-start; gap: var(--space-3); }

.spool-avatar {
  width: 40px;
  height: 40px;
  border-radius: var(--radius-md);
  background: var(--color-brand-subtle);
  color: var(--color-brand);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.title-row {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  flex-wrap: wrap;
}
.spool-title {
  font-size: var(--text-xl);
  font-weight: var(--weight-medium);
  margin: 0;
  color: var(--color-text-primary);
}
.spool-sub {
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
  margin: var(--space-1) 0 0;
}

.badge--overdue {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: var(--text-xs);
  font-weight: var(--weight-medium);
  padding: 3px 9px;
  border-radius: var(--radius-pill);
  background: var(--color-danger-bg);
  color: var(--color-danger);
}

.terminal-badge {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  font-size: var(--text-sm);
  color: var(--color-success);
  font-weight: var(--weight-medium);
}

/* ── Flow diagram ── */
.flow-card { margin-bottom: var(--space-4); }
.flow-scroll { overflow-x: auto; padding-bottom: var(--space-1); }

.flow {
  display: flex;
  align-items: flex-start;
  min-width: 560px;
  padding: var(--space-2) 0 var(--space-3);
}

.station-node {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-1);
  flex-shrink: 0;
  width: 80px;
}

.connector {
  flex: 1;
  height: 3px;
  margin-top: 19px;
  border-radius: var(--radius-pill);
  min-width: 16px;
}
.connector.done    { background: var(--color-brand); }
.connector.upcoming { background: var(--color-border); }

.node-circle {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 2px solid transparent;
  transition: transform var(--transition-fast);
}
.node-circle.done {
  background: var(--color-brand);
  color: var(--color-text-on-brand);
  border-color: var(--color-brand);
}
.node-circle.current {
  background: var(--color-bg-surface);
  border-color: var(--color-brand);
  color: var(--color-brand);
  box-shadow: 0 0 0 4px var(--color-brand-subtle);
}
.node-circle.upcoming {
  background: var(--color-bg-inset);
  border-color: var(--color-border);
  color: var(--color-text-tertiary);
}

.node-index {
  font-size: var(--text-sm);
  font-weight: var(--weight-medium);
}

.node-label {
  font-size: var(--text-xs);
  font-weight: var(--weight-medium);
  text-align: center;
}
.node-label.done    { color: var(--color-brand); }
.node-label.current { color: var(--color-brand); font-weight: 600; }
.node-label.upcoming { color: var(--color-text-tertiary); }

.node-date {
  font-size: 11px;
  color: var(--color-text-tertiary);
  text-align: center;
}

.current-pill {
  font-size: 11px;
  font-weight: var(--weight-medium);
  color: var(--color-text-on-brand);
  background: var(--color-brand);
  border-radius: var(--radius-pill);
  padding: 1px 8px;
}

/* ── Two-column layout ── */
.two-col {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-4);
  margin-bottom: var(--space-4);
}
@media (max-width: 600px) {
  .two-col { grid-template-columns: 1fr; }
}

/* ── Section titles ── */
.section-title {
  font-size: var(--text-base);
  font-weight: var(--weight-medium);
  color: var(--color-text-primary);
  margin: 0 0 var(--space-4);
}

/* ── Details list ── */
.details-list { display: flex; flex-direction: column; gap: 0; }
.detail-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--space-2) 0;
  border-bottom: 1px solid var(--color-border);
  gap: var(--space-3);
}
.detail-row:last-child { border-bottom: none; }
.detail-row dt {
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
  flex-shrink: 0;
}
.detail-row dd {
  font-size: var(--text-sm);
  color: var(--color-text-primary);
  font-weight: var(--weight-medium);
  margin: 0;
  text-align: right;
}
.detail-row dd.overdue { color: var(--color-danger); }
.mono { font-family: var(--font-mono); font-size: var(--text-xs) !important; }

/* ── BOM ── */
.bom-list { list-style: none; padding: 0; margin: 0; }
.bom-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--space-2) 0;
  border-bottom: 1px solid var(--color-border);
}
.bom-row:last-child { border-bottom: none; }
.bom-part {
  font-size: var(--text-sm);
  font-family: var(--font-mono);
  color: var(--color-text-primary);
}
.bom-qty {
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
}

/* ── History ── */
.history-list { list-style: none; padding: 0; margin: 0; }
.history-row {
  display: flex;
  align-items: flex-start;
  gap: var(--space-3);
  padding: var(--space-3) 0;
  border-bottom: 1px solid var(--color-border);
}
.history-row:last-child { border-bottom: none; }

.history-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  flex-shrink: 0;
  margin-top: 4px;
}
.station-dot--detailing { background: var(--station-detailing); }
.station-dot--cut       { background: var(--station-cut); }
.station-dot--weld      { background: var(--station-weld); }
.station-dot--qc        { background: var(--station-qc); }
.station-dot--shipped   { background: var(--station-shipped); }
.station-dot--installed { background: var(--station-installed); }

.history-body {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.history-station {
  font-size: var(--text-sm);
  font-weight: var(--weight-medium);
  color: var(--color-text-primary);
}
.history-meta {
  font-size: var(--text-xs);
  color: var(--color-text-secondary);
}

.empty-state {
  font-size: var(--text-sm);
  color: var(--color-text-tertiary);
  text-align: center;
  padding: var(--space-5) 0;
}

.loading {
  text-align: center;
  padding: var(--space-10) 0;
  font-size: var(--text-sm);
  color: var(--color-text-tertiary);
}

/* ── Card spacing ── */
.card { margin-bottom: var(--space-4); }
</style>
