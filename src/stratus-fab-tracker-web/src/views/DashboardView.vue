<script setup lang="ts">
import { onMounted, ref, computed } from 'vue';

type DashboardDto = {
  wipByStation: Record<string, number>;
  pastDueCount: number;
};

type ThroughputDayDto = { day: string; completed: number };
type ThroughputDto = {
  daily: ThroughputDayDto[];
  completedPerDay: number;
  duePerDay: number;
  keepingUp: boolean;
};

const dashboard = ref<DashboardDto | null>(null);
const throughput = ref<ThroughputDto | null>(null);
const error = ref('');

const STATIONS = ['Detailing', 'Cut', 'Weld', 'QC', 'Shipped', 'Installed'];

const totalWip = computed(() => {
  if (!dashboard.value) return 0;
  return Object.values(dashboard.value.wipByStation).reduce((a, b) => a + b, 0);
});

const maxDaily = computed(() =>
  throughput.value ? Math.max(...throughput.value.daily.map(d => d.completed), 1) : 1
);

async function load() {
  try {
    const [d, t] = await Promise.all([
      fetch('/api/dashboard').then(r => r.json()),
      fetch('/api/throughput').then(r => r.json()),
    ]);
    dashboard.value = d;
    throughput.value = t;
  } catch {
    error.value = 'Unable to load dashboard data. Make sure the API is running.';
  }
}

onMounted(load);
</script>

<template>
  <div>
    <div v-if="error" class="alert" role="alert">
      <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true" style="flex-shrink:0">
        <circle cx="8" cy="8" r="7" stroke="currentColor" stroke-width="1.4"/>
        <line x1="8" y1="5" x2="8" y2="8.5" stroke="currentColor" stroke-width="1.4" stroke-linecap="round"/>
        <circle cx="8" cy="11" r=".75" fill="currentColor"/>
      </svg>
      {{ error }}
    </div>

    <template v-if="dashboard && throughput">

      <!-- ── Metrics row ── -->
      <div class="metrics-grid" role="region" aria-label="Summary metrics">
        <div class="metric">
          <p class="metric__label">Total WIP</p>
          <p class="metric__value">{{ totalWip }}</p>
        </div>
        <div class="metric">
          <p class="metric__label">Past due</p>
          <p class="metric__value"
            :style="dashboard.pastDueCount > 0 ? 'color:var(--color-danger)' : ''">
            {{ dashboard.pastDueCount }}
          </p>
        </div>
        <div class="metric">
          <p class="metric__label">Completed / day</p>
          <p class="metric__value">{{ throughput.completedPerDay.toFixed(1) }}</p>
        </div>
        <div class="metric">
          <p class="metric__label">Keeping pace</p>
          <p class="metric__value"
            :style="throughput.keepingUp ? 'color:var(--color-success)' : 'color:var(--color-danger)'">
            {{ throughput.keepingUp ? 'Yes' : 'No' }}
          </p>
        </div>
      </div>

      <!-- ── WIP by station ── -->
      <section class="card" aria-labelledby="wip-heading">
        <h2 id="wip-heading" class="section-title">WIP by station</h2>
        <div class="station-list">
          <div v-for="station in STATIONS" :key="station" class="station-row">
            <span class="badge" :class="`station--${station.toLowerCase()}`">
              <span class="badge__dot" aria-hidden="true"></span>
              {{ station }}
            </span>
            <div class="bar-track" role="progressbar"
              :aria-valuenow="dashboard.wipByStation[station] ?? 0"
              :aria-valuemax="totalWip"
              :aria-label="`${station} WIP`">
              <div class="bar-fill" :class="`station-fill--${station.toLowerCase()}`"
                :style="{ width: totalWip > 0
                  ? ((dashboard.wipByStation[station] ?? 0) / totalWip * 100) + '%'
                  : '0%' }">
              </div>
            </div>
            <span class="count">{{ dashboard.wipByStation[station] ?? 0 }}</span>
          </div>
        </div>
      </section>

      <!-- ── Daily throughput ── -->
      <section class="card" aria-labelledby="throughput-heading">
        <div class="section-header">
          <h2 id="throughput-heading" class="section-title">Daily throughput</h2>
          <span class="section-meta">Due rate: {{ throughput.duePerDay.toFixed(1) }} / day</span>
        </div>
        <ul class="throughput-list" role="list">
          <li v-for="day in throughput.daily" :key="day.day" class="throughput-row">
            <span class="day-label">{{ day.day }}</span>
            <div class="bar-track">
              <div class="bar-fill brand-fill"
                :style="{ width: (day.completed / maxDaily * 100) + '%' }">
              </div>
            </div>
            <span class="count">{{ day.completed }}</span>
          </li>
        </ul>
      </section>

    </template>

    <p v-else-if="!error" class="loading" aria-live="polite">Loading…</p>
  </div>
</template>

<style scoped>
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

/* ── Metrics ── */
.metrics-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: var(--space-3);
  margin-bottom: var(--space-5);
}

/* ── Section headings ── */
.section-header {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: var(--space-3);
}
.section-title {
  font-size: var(--text-lg);
  font-weight: var(--weight-medium);
  margin: 0;
  color: var(--color-text-primary);
}
.section-meta {
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
}

/* ── Station bars ── */
.station-list { display: flex; flex-direction: column; gap: var(--space-2); }
.station-row {
  display: grid;
  grid-template-columns: 112px 1fr 32px;
  align-items: center;
  gap: var(--space-3);
}

/* ── Generic bar ── */
.bar-track {
  height: 8px;
  background: var(--color-bg-inset);
  border-radius: var(--radius-pill);
  overflow: hidden;
}
.bar-fill {
  height: 100%;
  border-radius: var(--radius-pill);
  transition: width 0.5s cubic-bezier(0.4, 0, 0.2, 1);
}
.station-fill--detailing { background: var(--station-detailing); }
.station-fill--cut       { background: var(--station-cut); }
.station-fill--weld      { background: var(--station-weld); }
.station-fill--qc        { background: var(--station-qc); }
.station-fill--shipped   { background: var(--station-shipped); }
.station-fill--installed { background: var(--station-installed); }
.brand-fill              { background: var(--color-brand); }

.count {
  font-size: var(--text-sm);
  font-weight: var(--weight-medium);
  color: var(--color-text-secondary);
  text-align: right;
}

/* ── Throughput list ── */
.throughput-list { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: var(--space-2); }
.throughput-row {
  display: grid;
  grid-template-columns: 90px 1fr 32px;
  align-items: center;
  gap: var(--space-3);
}
.day-label {
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
  font-family: var(--font-mono);
}

/* ── Loading ── */
.loading {
  text-align: center;
  padding: var(--space-10) 0;
  font-size: var(--text-sm);
  color: var(--color-text-tertiary);
}
</style>
