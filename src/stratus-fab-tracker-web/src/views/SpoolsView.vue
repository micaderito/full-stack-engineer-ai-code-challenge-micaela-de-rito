<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';

type BomItem = { partNumber: string; quantity: number };
type StatusEvent = { station: number; changedAt: string; changedBy: string };
type Spool = {
  id: string;
  packageId: string;
  spoolNumber: string;
  dueDate: string;
  bom: BomItem[];
  statusHistory: StatusEvent[];
  currentStation: number;
};

const STATIONS = ['Detailing', 'Cut', 'Weld', 'QC', 'Shipped', 'Installed'] as const;
const STATION_KEYS = ['detailing', 'cut', 'weld', 'qc', 'shipped', 'installed'] as const;

const router = useRouter();

const spools = ref<Spool[]>([]);
const search = ref('');
const error = ref('');
const advancing = ref<Set<string>>(new Set());

function goToDetail(spool: Spool) {
  router.push({ name: 'SpoolDetail', params: { id: spool.id } });
}

const today = new Date();
today.setHours(0, 0, 0, 0);

function isOverdue(spool: Spool) {
  if (spool.currentStation === 5) return false;
  return new Date(spool.dueDate) < today;
}

const filtered = computed(() => {
  const q = search.value.trim().toLowerCase();
  if (!q) return spools.value;
  return spools.value.filter(s =>
    s.spoolNumber.toLowerCase().includes(q) ||
    s.packageId.toLowerCase().includes(q) ||
    STATIONS[s.currentStation].toLowerCase().includes(q),
  );
});

const PAGE_SIZE = 20;
const currentPage = ref(1);

const totalPages = computed(() => Math.max(1, Math.ceil(filtered.value.length / PAGE_SIZE)));

const paged = computed(() => {
  const start = (currentPage.value - 1) * PAGE_SIZE;
  return filtered.value.slice(start, start + PAGE_SIZE);
});

const rangeStart = computed(() =>
  filtered.value.length === 0 ? 0 : (currentPage.value - 1) * PAGE_SIZE + 1,
);
const rangeEnd = computed(() =>
  Math.min(currentPage.value * PAGE_SIZE, filtered.value.length),
);

// Reset to the first page whenever the filter result shrinks/changes under us.
watch(filtered, () => {
  if (currentPage.value > totalPages.value) currentPage.value = totalPages.value;
});
watch(search, () => {
  currentPage.value = 1;
});

function goToPage(page: number) {
  currentPage.value = Math.min(Math.max(1, page), totalPages.value);
}

async function load() {
  try {
    const data = await fetch('/api/spools').then(r => r.json());
    spools.value = data;
  } catch {
    error.value = 'Unable to load spools. Make sure the API is running.';
  }
}

async function advance(spool: Spool) {
  if (advancing.value.has(spool.id)) return;
  advancing.value = new Set(advancing.value).add(spool.id);
  try {
    const res = await fetch(`/api/spools/${spool.id}/advance`, { method: 'POST' });
    if (res.ok) {
      spool.currentStation = Math.min(spool.currentStation + 1, 5);
    } else {
      const body = await res.json().catch(() => ({}));
      error.value = body.message ?? 'Could not advance spool.';
    }
  } catch {
    error.value = 'Network error while advancing spool.';
  } finally {
    const next = new Set(advancing.value);
    next.delete(spool.id);
    advancing.value = next;
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

    <div class="page-header">
      <div>
        <h1 class="page-title">Spools</h1>
        <p class="page-subtitle">All fabrication spools and their workflow status</p>
      </div>
    </div>

    <div class="search-wrap">
      <svg class="search-icon" width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
        <circle cx="6.5" cy="6.5" r="4.5" stroke="currentColor" stroke-width="1.4"/>
        <line x1="10" y1="10" x2="14" y2="14" stroke="currentColor" stroke-width="1.4" stroke-linecap="round"/>
      </svg>
      <input
        v-model="search"
        type="search"
        class="input search-input"
        placeholder="Search by spool number, package, or station…"
        aria-label="Search spools"
      />
    </div>

    <p class="count-label" aria-live="polite">
      <template v-if="filtered.length">
        Showing {{ rangeStart }}–{{ rangeEnd }} of {{ filtered.length }}
        <template v-if="filtered.length !== spools.length"> (filtered from {{ spools.length }})</template>
        spools
      </template>
      <template v-else>Showing 0 of {{ spools.length }} spools</template>
    </p>

    <div class="table-wrap card" role="region" aria-label="Spools table">
      <table>
        <colgroup>
          <col style="width:100px">
          <col style="width:90px">
          <col style="width:110px">
          <col style="width:140px">
          <col style="width:56px">
          <col>
        </colgroup>
        <thead>
          <tr>
            <th scope="col">Spool #</th>
            <th scope="col">Package</th>
            <th scope="col">Due date</th>
            <th scope="col">Station</th>
            <th scope="col">BOM</th>
            <th scope="col">Action</th>
          </tr>
        </thead>
        <tbody v-if="spools.length">
          <tr
            v-for="spool in paged"
            :key="spool.id"
            :class="`row--${STATION_KEYS[spool.currentStation]}`"
            class="row--clickable"
            @click="goToDetail(spool)"
          >
            <td>
              <span class="spool-num">{{ spool.spoolNumber }}</span>
            </td>
            <td class="cell-muted">{{ spool.packageId }}</td>
            <td>
              <span :class="isOverdue(spool) ? 'due due--overdue' : 'due'">
                {{ spool.dueDate }}
                <span v-if="isOverdue(spool)" class="overdue-tag">past due</span>
              </span>
            </td>
            <td>
              <span class="badge" :class="`station--${STATION_KEYS[spool.currentStation]}`">
                <span class="badge__dot" aria-hidden="true"></span>
                {{ STATIONS[spool.currentStation] }}
              </span>
            </td>
            <td class="cell-muted">{{ spool.bom.length }}</td>
            <td>
              <button
                v-if="spool.currentStation < 5"
                class="btn btn--secondary btn--sm"
                :disabled="advancing.has(spool.id)"
                @click.stop="advance(spool)"
              >
                <svg width="13" height="13" viewBox="0 0 13 13" fill="none" aria-hidden="true">
                  <path d="M2.5 6.5h8M7 3l3.5 3.5L7 10" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
                {{ STATIONS[spool.currentStation + 1] }}
              </button>
              <span v-else class="badge station--installed">
                <span class="badge__dot" aria-hidden="true"></span>
                Installed
              </span>
            </td>
          </tr>
          <tr v-if="filtered.length === 0">
            <td colspan="6" class="empty-cell">No spools match your search.</td>
          </tr>
        </tbody>
        <tbody v-else-if="!error">
          <tr>
            <td colspan="6" class="empty-cell">Loading…</td>
          </tr>
        </tbody>
      </table>
    </div>

    <nav v-if="totalPages > 1" class="pagination" aria-label="Spools pagination">
      <button
        class="btn btn--secondary btn--sm"
        :disabled="currentPage === 1"
        @click="goToPage(currentPage - 1)"
      >
        <svg width="13" height="13" viewBox="0 0 14 14" fill="none" aria-hidden="true">
          <path d="M8.5 2.5L4 7l4.5 4.5" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
        Previous
      </button>
      <span class="pagination__status">Page {{ currentPage }} of {{ totalPages }}</span>
      <button
        class="btn btn--secondary btn--sm"
        :disabled="currentPage === totalPages"
        @click="goToPage(currentPage + 1)"
      >
        Next
        <svg width="13" height="13" viewBox="0 0 14 14" fill="none" aria-hidden="true">
          <path d="M5.5 2.5L10 7l-4.5 4.5" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </button>
    </nav>
  </div>
</template>

<style scoped>
/* ── Page header ── */
.page-header {
  margin-bottom: var(--space-5);
}
.page-title {
  font-size: var(--text-2xl);
  font-weight: var(--weight-medium);
  margin: 0 0 var(--space-1);
  color: var(--color-text-primary);
}
.page-subtitle {
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
  margin: 0;
}

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

/* ── Search ── */
.search-wrap {
  position: relative;
  margin-bottom: var(--space-2);
}
.search-icon {
  position: absolute;
  left: var(--space-3);
  top: 50%;
  transform: translateY(-50%);
  color: var(--color-text-tertiary);
  pointer-events: none;
}
.search-input {
  padding-left: calc(var(--space-3) + 16px + var(--space-2));
}

/* ── Count label ── */
.count-label {
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
  margin-bottom: var(--space-3);
}

/* ── Table wrapper ── */
.table-wrap {
  padding: 0;
  overflow: hidden;
}
table {
  width: 100%;
  border-collapse: collapse;
  table-layout: fixed;
}
thead tr {
  border-bottom: 1px solid var(--color-border);
}
thead th {
  padding: var(--space-3) var(--space-4);
  text-align: left;
  font-size: var(--text-xs);
  font-weight: var(--weight-medium);
  color: var(--color-text-secondary);
  background: var(--color-bg-surface-alt);
  white-space: nowrap;
}
tbody tr {
  border-bottom: 1px solid var(--color-border);
  transition: filter var(--transition-fast);
}
tbody tr:last-child {
  border-bottom: none;
}
tbody tr:hover {
  filter: brightness(0.97);
}
.row--clickable {
  cursor: pointer;
}
td {
  padding: var(--space-3) var(--space-4);
  font-size: var(--text-sm);
  vertical-align: middle;
}

/* ── Row station tints ── */
.row--detailing { background: var(--station-detailing-bg); }
.row--cut       { background: var(--station-cut-bg); }
.row--weld      { background: var(--station-weld-bg); }
.row--qc        { background: var(--station-qc-bg); }
.row--shipped   { background: var(--station-shipped-bg); }
.row--installed { background: var(--station-installed-bg); }

/* ── Cell styles ── */
.spool-num {
  font-family: var(--font-mono);
  font-size: var(--text-xs);
  font-weight: var(--weight-medium);
}
.cell-muted {
  color: var(--color-text-secondary);
  font-size: var(--text-xs);
}
.due {
  font-size: var(--text-xs);
  color: var(--color-text-secondary);
}
.due--overdue {
  color: var(--color-warning);
  font-weight: var(--weight-medium);
}
.overdue-tag {
  display: block;
  font-size: 11px;
  color: var(--color-warning);
  font-weight: var(--weight-regular);
}

/* ── Advance button (small secondary) ── */
.btn--sm {
  height: 28px;
  padding: 0 var(--space-3);
  font-size: var(--text-xs);
  gap: var(--space-1);
}

/* ── Empty state ── */
.empty-cell {
  text-align: center;
  padding: var(--space-10) var(--space-4);
  color: var(--color-text-tertiary);
  font-size: var(--text-sm);
}

/* ── Pagination ── */
.pagination {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-4);
  margin-top: var(--space-4);
}
.pagination__status {
  font-size: var(--text-sm);
  color: var(--color-text-secondary);
  min-width: 110px;
  text-align: center;
}
</style>
