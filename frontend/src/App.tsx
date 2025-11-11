import { useCallback, useEffect, useRef, useState } from 'react'
import './App.css'
import ProductForm from './components/ProductForm'
import ProductList from './components/ProductList'
import ExpiringList from './components/ExpiringList'
import SummaryPanel from './components/SummaryPanel'
import RepairForm from './components/RepairForm'
import RepairList from './components/RepairList'
import {
  ApiError,
  createProduct,
  createRepair,
  deleteProduct,
  fetchExpiringProducts,
  fetchProducts,
  fetchRepairs,
  fetchSummaryReport,
  fetchWarrantyStatus,
  updateRepairStatus,
} from './api'
import type {
  CreateProductPayload,
  CreateRepairPayload,
  ExpiringProductResponse,
  Product,
  Repair,
  RepairStatus,
  SummaryReport,
  WarrantyStatusResponse,
} from './api'

const DEFAULT_EXPIRING_WINDOW = 45

type LoadMode = 'initial' | 'refresh'
type RepairFilter = RepairStatus | 'All'

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
  const [repairs, setRepairs] = useState<Repair[]>([])
  const [repairsLoading, setRepairsLoading] = useState(true)
  const [repairsRefreshing, setRepairsRefreshing] = useState(false)
  const [repairError, setRepairError] = useState<string | null>(null)
  const [repairStatusFilter, setRepairStatusFilter] = useState<RepairFilter>('Open')
  const [isCreatingRepair, setIsCreatingRepair] = useState(false)
  const [repairFormError, setRepairFormError] = useState<string | null>(null)
  const [repairFormSuccess, setRepairFormSuccess] = useState<string | null>(null)
  const [repairActionErrors, setRepairActionErrors] = useState<Record<number, string>>({})
  const [repairActionLoading, setRepairActionLoading] = useState<Record<number, boolean>>({})
  const [productDeleteErrors, setProductDeleteErrors] = useState<Record<number, string>>({})
  const [productDeleteLoading, setProductDeleteLoading] = useState<Record<number, boolean>>({})

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

  const loadRepairs = useCallback(
    async (statusOverride?: RepairFilter, mode: LoadMode = 'refresh') => {
      const appliedFilter = statusOverride ?? repairStatusFilter

      if (mode === 'initial') {
        setRepairsLoading(true)
      } else {
        setRepairsRefreshing(true)
      }

      try {
        const repairsData = await fetchRepairs(appliedFilter === 'All' ? undefined : appliedFilter)
        setRepairs(repairsData)
        setRepairError(null)
        setRepairStatusFilter((prev) => (prev === appliedFilter ? prev : appliedFilter))
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Unable to load repairs. Check the API logs for details.'
        setRepairError(message)
      } finally {
        if (mode === 'initial') {
          setRepairsLoading(false)
        } else {
          setRepairsRefreshing(false)
        }
      }
    },
    [repairStatusFilter],
  )

  useEffect(() => {
    void loadRepairs('Open', 'initial')
  }, [loadRepairs])

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

  const handleCreateRepair = async (payload: CreateRepairPayload) => {
    setIsCreatingRepair(true)
    setRepairFormError(null)
    setRepairFormSuccess(null)

    try {
      const repair = await createRepair(payload)
      const productName = products.find((product) => product.id === repair.productId)?.name ?? `product #${repair.productId}`
      setRepairFormSuccess(`Opened repair #${repair.id} for ${productName}.`)
      await Promise.all([loadRepairs(undefined, 'refresh'), loadData(expiringDays, 'refresh')])
      return true
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Unable to open the repair. Check the API logs for details.'
      setRepairFormError(message)
      return false
    } finally {
      setIsCreatingRepair(false)
    }
  }

  const handleUpdateRepairStatus = async (repairId: number, nextStatus: RepairStatus) => {
    setRepairActionErrors((prev) => {
      const next = { ...prev }
      delete next[repairId]
      return next
    })
    setRepairActionLoading((prev) => ({ ...prev, [repairId]: true }))

    try {
      await updateRepairStatus(repairId, nextStatus)
      await Promise.all([loadRepairs(undefined, 'refresh'), loadData(expiringDays, 'refresh')])
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : 'Unable to update the repair status. Check the API logs for details.'
      setRepairActionErrors((prev) => ({ ...prev, [repairId]: message }))
    } finally {
      setRepairActionLoading((prev) => {
        const next = { ...prev }
        delete next[repairId]
        return next
      })
    }
  }

  const handleRepairFilterChange = async (nextFilter: RepairFilter) => {
    if (nextFilter === repairStatusFilter && !repairsRefreshing) {
      return
    }

    await loadRepairs(nextFilter, 'refresh')
  }

  const handleRefreshRepairs = async () => {
    await loadRepairs(undefined, 'refresh')
  }

  const handleDeleteProduct = async (productId: number) => {
    const target = products.find((product) => product.id === productId)
    const confirmed = window.confirm(
      `Delete "${target?.name ?? 'this product'}"? This also removes any associated repairs.`,
    )

    if (!confirmed) {
      return
    }

    setProductDeleteErrors((prev) => {
      const next = { ...prev }
      delete next[productId]
      return next
    })
    setProductDeleteLoading((prev) => ({ ...prev, [productId]: true }))

    try {
      await deleteProduct(productId)
      setWarrantyStatuses((prev) => {
        const next = { ...prev }
        delete next[productId]
        return next
      })
      await Promise.all([loadData(expiringDays, 'refresh'), loadRepairs(undefined, 'refresh')])
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Unable to delete the product. Check the API logs for details.'
      setProductDeleteErrors((prev) => ({ ...prev, [productId]: message }))
    } finally {
      setProductDeleteLoading((prev) => {
        const next = { ...prev }
        delete next[productId]
        return next
      })
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
            onDeleteProduct={(productId) => {
              void handleDeleteProduct(productId)
            }}
            deleteLoading={productDeleteLoading}
            deleteErrors={productDeleteErrors}
          />

          <div className="grid">
            <section className="card">
              <div className="card-header">
                <div>
                  <h2>Open a repair</h2>
                  <p className="muted">Create a case before it escalates.</p>
                </div>
              </div>
              <RepairForm
                products={products}
                isSubmitting={isCreatingRepair}
                onCreate={handleCreateRepair}
                serverError={repairFormError}
                successMessage={repairFormSuccess}
              />
            </section>

            <ExpiringList
              items={expiring}
              currentWindow={expiringDays}
              inputValue={expiringInput}
              inputError={expiringInputError}
              isRefreshing={refreshing}
              onInputChange={handleExpiringInputChange}
              onApplyWindow={handleApplyExpiringWindow}
            />
          </div>

          <RepairList
            repairs={repairs}
            products={products}
            filter={repairStatusFilter}
            isLoading={repairsLoading}
            isRefreshing={repairsRefreshing}
            error={repairError}
            onFilterChange={(next) => {
              void handleRepairFilterChange(next)
            }}
            onRefresh={() => {
              void handleRefreshRepairs()
            }}
            onAdvanceStatus={(repairId, status) => {
              void handleUpdateRepairStatus(repairId, status)
            }}
            actionErrors={repairActionErrors}
            actionLoading={repairActionLoading}
          />
        </>
      )}
    </div>
  )
}

export default App
