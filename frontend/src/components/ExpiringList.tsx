import type { ExpiringProductResponse } from '../api'

interface ExpiringListProps {
  items: ExpiringProductResponse[]
  currentWindow: number
  inputValue: string
  inputError?: string | null
  isRefreshing: boolean
  onInputChange: (value: string) => void
  onApplyWindow: () => Promise<void>
}

const formatDate = (value: string) => {
  if (!value) return '--'
  return new Date(value).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

const ExpiringList = ({
  items,
  currentWindow,
  inputValue,
  inputError,
  isRefreshing,
  onInputChange,
  onApplyWindow,
}: ExpiringListProps) => {
  const handleApply = () => {
    void onApplyWindow()
  }

  return (
    <section className="card">
      <div className="card-header expiring-header">
        <div>
          <h2>Expiring soon</h2>
          <p className="muted">Within the next {currentWindow} day(s)</p>
        </div>
        <div className="inline-controls">
          <label>
            <span>Window</span>
            <input
              type="number"
              min={1}
              value={inputValue}
              onChange={(event) => onInputChange(event.target.value)}
            />
          </label>
          <button type="button" onClick={handleApply} disabled={isRefreshing}>
            {isRefreshing ? 'Updating...' : 'Apply'}
          </button>
        </div>
      </div>
      {inputError && <p className="form-hint error">{inputError}</p>}
      {items.length === 0 ? (
        <p className="muted">No warranties expire in that window.</p>
      ) : (
        <ul className="expiring-list">
          {items.map(({ product, daysRemaining }) => (
            <li key={product.id} className="expiring-item">
              <div>
                <p className="item-title">{product.name}</p>
                <p className="muted">
                  Serial {product.serial} • purchased {formatDate(product.purchaseDate)} • warranty {product.warrantyMonths} mo
                </p>
              </div>
              <span className="tag">{daysRemaining} day(s) left</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

export default ExpiringList
