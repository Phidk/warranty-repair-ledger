import { useCallback, useEffect, useRef, useState } from 'react'
import './App.css'
import ProductForm from './components/ProductForm'
import ProductList from './components/ProductList'
import ExpiringList from './components/ExpiringList'
import SummaryPanel from './components/SummaryPanel'
import { ApiError, createProduct, fetchExpiringProducts, fetchProducts, fetchSummaryReport, fetchWarrantyStatus } from './api'
import type {
  CreateProductPayload,
  ExpiringProductResponse,
  Product,
  SummaryReport,
  WarrantyStatusResponse,
} from './api'

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
  const productQueryRef = useRef('')
  const [productQuery, setProductQuery] = useState(productQueryRef.current)
  const [productSearchInput, setProductSearchInput] = useState(productQueryRef.current)
  const [warrantyStatuses, setWarrantyStatuses] = useState<Record<number, WarrantyStatusResponse>>({})
  const [warrantyErrors, setWarrantyErrors] = useState<Record<number, string>>({})
  const [warrantyLoading, setWarrantyLoading] = useState<Record<number, boolean>>({})

  const loadData = useCallback(async (days: number, mode: LoadMode = 'refresh', queryOverride?: string) => {
    const appliedQuery = queryOverride ?? productQueryRef.current

    if (mode === 'initial') {
      setLoading(true)
    } else {
      setRefreshing(true)
    }

    try {
      const [productsData, expiringData, summaryData] = await Promise.all([
        fetchProducts(appliedQuery),
        fetchExpiringProducts(days),
        fetchSummaryReport(),
      ])
      setProducts(productsData)
      setExpiring(expiringData)
      setSummary(summaryData)
      if (productQueryRef.current !== appliedQuery) {
        productQueryRef.current = appliedQuery
      }
      setProductQuery((prev) => (prev === appliedQuery ? prev : appliedQuery))
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

  const handleSearchInputChange = (value: string) => {
    setProductSearchInput(value)
  }

  const handleApplyProductSearch = async () => {
    if (refreshing) {
      return
    }

    const trimmed = productSearchInput.trim()
    if (trimmed === productQuery) {
      return
    }

    setProductSearchInput(trimmed)
    await loadData(expiringDays, 'refresh', trimmed)
  }

  const handleClearProductSearch = async () => {
    if (refreshing) {
      return
    }

    if (!productQuery && productSearchInput.length === 0) {
      return
    }

    setProductSearchInput('')
    await loadData(expiringDays, 'refresh', '')
  }

  const handleCheckWarranty = async (productId: number) => {
    setWarrantyErrors((prev) => {
      const next = { ...prev }
      delete next[productId]
      return next
    })
    setWarrantyLoading((prev) => ({ ...prev, [productId]: true }))

    try {
      const status = await fetchWarrantyStatus(productId)
      setWarrantyStatuses((prev) => ({ ...prev, [productId]: status }))
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Unable to check warranty status. See server logs for details.'
      setWarrantyErrors((prev) => ({ ...prev, [productId]: message }))
    } finally {
      setWarrantyLoading((prev) => {
        const next = { ...prev }
        delete next[productId]
        return next
      })
    }
  }

  const showInitialLoader = loading && !products.length && !summary && !expiring.length
  const trimmedProductSearch = productSearchInput.trim()
  const canApplyProductSearch = trimmedProductSearch !== productQuery
  const canClearProductSearch = Boolean(productQuery) || productSearchInput.length > 0

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

          <ProductList
            products={products}
            isRefreshing={refreshing}
            searchValue={productSearchInput}
            appliedQuery={productQuery}
            onSearchInputChange={handleSearchInputChange}
            onApplySearch={() => {
              void handleApplyProductSearch()
            }}
            onClearSearch={() => {
              void handleClearProductSearch()
            }}
            canApplySearch={canApplyProductSearch}
            canClearSearch={canClearProductSearch}
            warrantyStatuses={warrantyStatuses}
            warrantyErrors={warrantyErrors}
            warrantyLoading={warrantyLoading}
            onCheckWarranty={(productId) => {
              void handleCheckWarranty(productId)
            }}
          />

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
