import { useMemo } from 'react'
import type { Product, Repair, RepairStatus } from '../api'

export type RepairFilter = RepairStatus | 'All'

interface RepairListProps {
  repairs: Repair[]
  products: Product[]
  filter: RepairFilter
  isLoading: boolean
  isRefreshing: boolean
  error: string | null
  onFilterChange: (filter: RepairFilter) => void
  onRefresh: () => void
  onAdvanceStatus: (repairId: number, nextStatus: RepairStatus) => void
  actionErrors: Record<number, string>
  actionLoading: Record<number, boolean>
}

const filterOptions: RepairFilter[] = ['All', 'Open', 'InProgress', 'Fixed', 'Rejected']

const statusTagClass = (status: RepairStatus) => {
  switch (status) {
    case 'Fixed':
      return 'tag-success'
    case 'Rejected':
      return 'tag-danger'
    default:
      return ''
  }
}

const getNextStatuses = (status: RepairStatus): RepairStatus[] => {
  switch (status) {
    case 'Open':
      return ['InProgress']
    case 'InProgress':
      return ['Fixed', 'Rejected']
    default:
      return []
  }
}

const formatDateTime = (value: string | null | undefined) => {
  if (!value) return '--'
  return new Date(value).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' })
}

const formatActionLabel = (status: RepairStatus) => {
  switch (status) {
    case 'InProgress':
      return 'Mark in progress'
    case 'Fixed':
      return 'Mark fixed'
    case 'Rejected':
      return 'Mark rejected'
    default:
      return `Mark ${status}`
  }
}

const RepairList = ({
  repairs,
  products,
  filter,
  isLoading,
  isRefreshing,
  error,
  onFilterChange,
  onRefresh,
  onAdvanceStatus,
  actionErrors,
  actionLoading,
}: RepairListProps) => {
  const productLookup = useMemo(() => {
    const map = new Map<number, Product>()
    products.forEach((product) => {
      map.set(product.id, product)
    })
    return map
  }, [products])

  const showInitialLoader = isLoading && repairs.length === 0

  return (
    <section className="card">
      <div className="card-header">
        <div>
          <h2>Repairs</h2>
          <p className="muted">Filter repairs and advance their status without leaving the browser.</p>
        </div>
        <div className="inline-controls">
          <label>
            <span>Status filter</span>
            <select
              value={filter}
              onChange={(event) => onFilterChange(event.target.value as RepairFilter)}
              disabled={isRefreshing}
            >
              {filterOptions.map((option) => (
                <option key={option} value={option}>
                  {option === 'All' ? 'All statuses' : option}
                </option>
              ))}
            </select>
          </label>
          <button type="button" onClick={() => onRefresh()} disabled={isRefreshing}>
            {isRefreshing ? 'Refreshing...' : 'Refresh'}
          </button>
        </div>
      </div>

      {error && <p className="form-hint error">{error}</p>}

      {showInitialLoader ? (
        <p>Loading repairs...</p>
      ) : repairs.length === 0 ? (
        <p className="muted">No repairs match this filter.</p>
      ) : (
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>Product</th>
                <th>Status</th>
                <th>Opened / Closed</th>
                <th>Notes</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {repairs.map((repair) => {
                const product = productLookup.get(repair.productId)
                const nextStatuses = getNextStatuses(repair.status)
                const isUpdating = Boolean(actionLoading[repair.id])
                const actionError = actionErrors[repair.id]

                return (
                  <tr key={repair.id}>
                    <td>{repair.id}</td>
                    <td>
                      <div className="repair-product">
                        <span className="table-title">{product?.name ?? `Product #${repair.productId}`}</span>
                        <p className="repair-meta">
                          #{repair.productId}
                          {product?.serial ? ` • ${product.serial}` : ''}
                        </p>
                      </div>
                    </td>
                    <td>
                      <div className="repair-status">
                        <span className={`tag ${statusTagClass(repair.status)}`}>{repair.status}</span>
                        {repair.consumerOptedForRepair && <p className="repair-meta">Right to Repair extension</p>}
                      </div>
                    </td>
                    <td>
                      <p className="repair-meta">Opened {formatDateTime(repair.openedAt)}</p>
                      <p className="repair-meta">
                        Closed {repair.closedAt ? formatDateTime(repair.closedAt) : '—'}
                      </p>
                    </td>
                    <td>
                      <p className="warranty-reason">{repair.notes ?? 'No notes captured.'}</p>
                    </td>
                    <td>
                      {nextStatuses.length === 0 ? (
                        <p className="repair-meta">Already closed.</p>
                      ) : (
                        <div className="action-stack">
                          {nextStatuses.map((status) => (
                            <button
                              key={status}
                              type="button"
                              className={`button-small ${status === 'Rejected' ? 'button-danger' : 'button-secondary'}`}
                              onClick={() => onAdvanceStatus(repair.id, status)}
                              disabled={isRefreshing || isUpdating}
                            >
                              {isUpdating ? 'Updating...' : formatActionLabel(status)}
                            </button>
                          ))}
                        </div>
                      )}
                      {actionError && <p className="form-hint error">{actionError}</p>}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

export default RepairList
