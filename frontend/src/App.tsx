import { useCallback, useEffect, useState } from 'react'
import './App.css'
import ProductForm from './components/ProductForm'
import ProductList from './components/ProductList'
import ExpiringList from './components/ExpiringList'
import SummaryPanel from './components/SummaryPanel'
import { ApiError, createProduct, fetchExpiringProducts, fetchProducts, fetchSummaryReport } from './api'
import type { CreateProductPayload, ExpiringProductResponse, Product, SummaryReport } from './api'

const DEFAULT_EXPIRING_WINDOW = 45

type LoadMode = 'initial' | 'refresh'

function App() {
  const [products, setProducts] = useState<Product[]>([])
  const [expiring, setExpiring] = useState<ExpiringProductResponse[]>([])
  const [summary, setSummary] = useState<SummaryReport | null>(null)
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [formSuccess, setFormSuccess] = useState<string | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [expiringDays, setExpiringDays] = useState(DEFAULT_EXPIRING_WINDOW)
  const [expiringInput, setExpiringInput] = useState(DEFAULT_EXPIRING_WINDOW.toString())
  const [expiringInputError, setExpiringInputError] = useState<string | null>(null)
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null)

  const loadData = useCallback(async (days: number, mode: LoadMode = 'refresh') => {
    if (mode === 'initial') {
      setLoading(true)
    } else {
      setRefreshing(true)
    }

    try {
      const [productsData, expiringData, summaryData] = await Promise.all([
        fetchProducts(),
        fetchExpiringProducts(days),
        fetchSummaryReport(),
      ])
      setProducts(productsData)
      setExpiring(expiringData)
      setSummary(summaryData)
      setError(null)
      setLastUpdated(new Date())
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : 'Unable to reach the API. Is `dotnet watch run` listening on http://localhost:8080?'
      setError(message)
    } finally {
      if (mode === 'initial') {
        setLoading(false)
      } else {
        setRefreshing(false)
      }
    }
  }, [])

  useEffect(() => {
    void loadData(DEFAULT_EXPIRING_WINDOW, 'initial')
  }, [loadData])

  const handleRefresh = useCallback(async () => {
    await loadData(expiringDays, 'refresh')
  }, [expiringDays, loadData])

  const handleCreateProduct = async (payload: CreateProductPayload) => {
    setIsCreating(true)
    setFormError(null)
    setFormSuccess(null)

    try {
      await createProduct(payload)
      setFormSuccess(`Saved "${payload.name}".`)
      await loadData(expiringDays, 'refresh')
      return true
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : 'Unable to create the product. Check the API logs for details.'
      setFormError(message)
      return false
    } finally {
      setIsCreating(false)
    }
  }

  const handleExpiringInputChange = (value: string) => {
    setExpiringInput(value)
    setExpiringInputError(null)
  }

  const handleApplyExpiringWindow = async () => {
    const parsed = Number(expiringInput)
    if (!Number.isFinite(parsed) || parsed <= 0) {
      setExpiringInputError('Enter a positive number of days.')
      return
    }

    setExpiringDays(parsed)
    await loadData(parsed, 'refresh')
  }

  const showInitialLoader = loading && !products.length && !summary && !expiring.length

  return (
    <div className="app-shell">
      <header className="app-header">
        <div>
          <p className="eyebrow">Warranty &amp; Repair Ledger</p>
          <h1>Local-first tracker</h1>
          <p className="muted">Capture purchases and repairs through a friendly UI backed by the same API.</p>
        </div>
        <div className="header-actions">
          {refreshing && <span className="tag">Syncing...</span>}
          <button type="button" onClick={() => void handleRefresh()} disabled={refreshing}>
            {refreshing ? 'Refreshing...' : 'Refresh data'}
          </button>
        </div>
      </header>

      {error && <div className="alert error">{error}</div>}

      {showInitialLoader ? (
        <section className="card">
          <p>Loading data...</p>
        </section>
      ) : (
        <>
          <div className="grid">
            <section className="card">
              <div className="card-header">
                <div>
                  <h2>Add a product</h2>
                  <p className="muted">Mirrors POST /products</p>
                </div>
              </div>
              <ProductForm
                isSubmitting={isCreating}
                onCreate={handleCreateProduct}
                serverError={formError}
                successMessage={formSuccess}
              />
            </section>

            <SummaryPanel summary={summary} isRefreshing={refreshing} lastUpdated={lastUpdated} onRefresh={handleRefresh} />
          </div>

          <ProductList products={products} />

          <ExpiringList
            items={expiring}
            currentWindow={expiringDays}
            inputValue={expiringInput}
            inputError={expiringInputError}
            isRefreshing={refreshing}
            onInputChange={handleExpiringInputChange}
            onApplyWindow={handleApplyExpiringWindow}
          />
        </>
      )}
    </div>
  )
}

export default App
