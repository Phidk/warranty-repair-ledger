import type { Product, WarrantyStatusResponse } from '../api'

interface ProductListProps {
  products: Product[]
  isRefreshing: boolean
  searchValue: string
  appliedQuery: string
  onSearchInputChange: (value: string) => void
  onApplySearch: () => void
  onClearSearch: () => void
  canApplySearch: boolean
  canClearSearch: boolean
  onCheckWarranty: (productId: number) => void
  warrantyStatuses: Record<number, WarrantyStatusResponse>
  warrantyErrors: Record<number, string>
  warrantyLoading: Record<number, boolean>
}

const formatDate = (value: string) => {
  if (!value) return '--'
  return new Date(value).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

const formatPrice = (value?: number | null) => {
  if (value === null || value === undefined) {
    return '--'
  }
  return new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value)
}

const ProductList = ({
  products,
  isRefreshing,
  searchValue,
  appliedQuery,
  onSearchInputChange,
  onApplySearch,
  onClearSearch,
  canApplySearch,
  canClearSearch,
  onCheckWarranty,
  warrantyStatuses,
  warrantyErrors,
  warrantyLoading,
}: ProductListProps) => {
  const handleApplySearch = () => {
    if (!canApplySearch || isRefreshing) {
      return
    }
    onApplySearch()
  }

  const handleClearSearch = () => {
    if (!canClearSearch || isRefreshing) {
      return
    }
    onClearSearch()
  }

  return (
    <section className="card">
      <div className="card-header">
        <div>
          <h2>Products</h2>
          <p className="muted">
            {products.length === 0
              ? 'No tracked purchases yet.'
              : `Showing ${products.length} item(s)${appliedQuery ? ` matching "${appliedQuery}"` : ''}.`}
          </p>
        </div>
      </div>

      <div className="search-controls">
        <label>
          <span>Search by name or serial</span>
          <input
            type="text"
            value={searchValue}
            onChange={(event) => onSearchInputChange(event.target.value)}
            placeholder="Laptop, SN123..."
          />
        </label>
        <div className="search-actions">
          <button type="button" onClick={handleApplySearch} disabled={!canApplySearch || isRefreshing}>
            {isRefreshing ? 'Working...' : appliedQuery ? 'Update search' : 'Apply search'}
          </button>
          <button type="button" onClick={handleClearSearch} disabled={!canClearSearch || isRefreshing}>
            Clear
          </button>
        </div>
      </div>

      {products.length === 0 ? (
        <p className="muted">Use the form above to add your first purchase - data stays in your local SQLite DB.</p>
      ) : (
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>Name</th>
                <th>Serial</th>
                <th>Brand</th>
                <th>Purchase date</th>
                <th>Warranty (months)</th>
                <th>Retailer</th>
                <th>Price</th>
                <th>Warranty status</th>
              </tr>
            </thead>
            <tbody>
              {products.map((product) => {
                const status = warrantyStatuses[product.id]
                const statusError = warrantyErrors[product.id]
                const isChecking = Boolean(warrantyLoading[product.id])

                return (
                  <tr key={product.id}>
                    <td>{product.id}</td>
                    <td>
                      <span className="table-title">{product.name}</span>
                    </td>
                    <td>{product.serial}</td>
                    <td>{product.brand ?? '--'}</td>
                    <td>{formatDate(product.purchaseDate)}</td>
                    <td>{product.warrantyMonths}</td>
                    <td>{product.retailer ?? '--'}</td>
                    <td>{formatPrice(product.price)}</td>
                    <td>
                      <div className="warranty-cell">
                        {status ? (
                          <>
                            <span className={`tag ${status.inWarranty ? 'tag-success' : 'tag-danger'}`}>
                              {status.inWarranty ? 'In warranty' : 'Out of warranty'}
                            </span>
                            <p className="warranty-meta">Expires {formatDate(status.expiresOn)}</p>
                            <p className="warranty-reason">{status.reason}</p>
                          </>
                        ) : (
                          <p className="warranty-meta muted">Run a check to view coverage status.</p>
                        )}
                        {statusError && <p className="form-hint error">{statusError}</p>}
                        <button type="button" onClick={() => onCheckWarranty(product.id)} disabled={isChecking}>
                          {isChecking ? 'Checking...' : 'Check warranty'}
                        </button>
                      </div>
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

export default ProductList
