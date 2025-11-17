import { useState } from 'react'
import type { ChangeEvent, FormEvent } from 'react'
import type { CreateRepairPayload, Product } from '../api'

const createInitialState = () => ({
  productId: '',
  notes: '',
})

type FormState = ReturnType<typeof createInitialState>

interface RepairFormProps {
  products: Product[]
  isSubmitting: boolean
  onCreate: (payload: CreateRepairPayload) => Promise<boolean>
  serverError?: string | null
  successMessage?: string | null
}

const RepairForm = ({ products, isSubmitting, onCreate, serverError, successMessage }: RepairFormProps) => {
  const [formState, setFormState] = useState<FormState>(createInitialState)
  const [touched, setTouched] = useState(false)

  const handleSelectChange = (event: ChangeEvent<HTMLSelectElement>) => {
    setFormState((prev) => ({ ...prev, productId: event.target.value }))
  }

  const handleNotesChange = (event: ChangeEvent<HTMLTextAreaElement>) => {
    setFormState((prev) => ({ ...prev, notes: event.target.value }))
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setTouched(true)

    if (!formState.productId) {
      return
    }

    const payload: CreateRepairPayload = {
      productId: Number(formState.productId),
    }

    if (formState.notes.trim()) {
      payload.notes = formState.notes.trim()
    }

    const saved = await onCreate(payload)
    if (saved) {
      setTouched(false)
      setFormState((prev) => ({
        productId: prev.productId,
        notes: '',
      }))
    }
  }

  const showValidationWarning = touched && !formState.productId
  const hasProducts = products.length > 0

  return (
    <form className="product-form" onSubmit={handleSubmit}>
      <div className="form-grid">
        <label>
          <span>Product *</span>
          <select name="productId" value={formState.productId} onChange={handleSelectChange} disabled={!hasProducts}>
            <option value="">Select a product</option>
            {products.map((product) => (
              <option key={product.id} value={product.id}>
                {product.name} (#{product.id})
              </option>
            ))}
          </select>
        </label>
        <label>
          <span>Notes</span>
          <textarea
            name="notes"
            value={formState.notes}
            placeholder="Describe the issue..."
            onChange={handleNotesChange}
            rows={3}
          />
        </label>
      </div>

      {!hasProducts && <p className="form-hint warning">Add a product first to open a repair.</p>}
      {showValidationWarning && <p className="form-hint warning">Choose a product to continue.</p>}
      {serverError && <p className="form-hint error">{serverError}</p>}
      {successMessage && <p className="form-hint success">{successMessage}</p>}

      <div className="form-actions">
        <button type="submit" disabled={isSubmitting || !hasProducts}>
          {isSubmitting ? 'Opening...' : 'Open repair'}
        </button>
      </div>
    </form>
  )
}

export default RepairForm
