import type { SummaryReport } from '../api'

interface SummaryPanelProps {
  summary: SummaryReport | null
  isRefreshing: boolean
  lastUpdated: Date | null
  onRefresh: () => Promise<void>
}

const statusOrder = ['Open', 'InProgress', 'Fixed', 'Rejected']

const SummaryPanel = ({ summary, isRefreshing, lastUpdated, onRefresh }: SummaryPanelProps) => {
  const handleRefresh = () => {
    void onRefresh()
  }

  const statusEntries = (() => {
    if (!summary) return []
    const extras = Object.keys(summary.countsByStatus).filter((key) => !statusOrder.includes(key))
    const ordered = [...statusOrder, ...extras]
    return ordered
      .filter((status) => summary.countsByStatus[status] !== undefined)
      .map((status) => ({ status, count: summary.countsByStatus[status] }))
  })()

  const avgDays = summary?.averageDaysOpen ?? null
  const updatedText = lastUpdated ? lastUpdated.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '--'

  return (
    <section className="card">
      <div className="card-header">
        <div>
          <h2>Snapshot</h2>
          <p className="muted">Quick signal before diving into the ledger.</p>
        </div>
        <button type="button" onClick={handleRefresh} disabled={isRefreshing}>
          {isRefreshing ? 'Refreshing...' : 'Refresh'}
        </button>
      </div>

      {!summary ? (
        <p className="muted">Summary data appears after the backend runs once.</p>
      ) : (
        <div className="summary-grid">
          <div className="summary-stack">
            <p className="summary-label">Repairs by status</p>
            <div className="status-grid">
              {statusEntries.map(({ status, count }) => (
                <div key={status} className="status-chip">
                  <span>{status}</span>
                  <strong>{count}</strong>
                </div>
              ))}
            </div>
          </div>
          <div className="summary-stack">
            <p className="summary-label">Average days open</p>
            <p className="summary-value">{avgDays === null ? '--' : avgDays.toFixed(1)}</p>
          </div>
          <div className="summary-stack">
            <p className="summary-label">Expiring warranties (30 day report)</p>
            <p className="summary-value">{summary.expiringProducts}</p>
          </div>
        </div>
      )}

      <p className="muted">Last updated {updatedText}</p>
    </section>
  )
}

export default SummaryPanel
