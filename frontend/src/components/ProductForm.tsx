import { useState } from 'react'
import type { ChangeEvent, FormEvent } from 'react'
import type { CreateProductPayload } from '../api'

const createInitialState = () => ({
  name: '',
  serial: '',
  purchaseDate: new Date().toISOString().split('T')[0],
  warrantyMonths: '24',
  brand: '',
  retailer: '',
  price: '',
})

type FormState = ReturnType<typeof createInitialState>

interface ProductFormProps {
  isSubmitting: boolean
  onCreate: (payload: CreateProductPayload) => Promise<boolean>
  serverError?: string | null
  successMessage?: string | null
}

const ProductForm = ({ isSubmitting, onCreate, serverError, successMessage }: ProductFormProps) => {
  const [formState, setFormState] = useState<FormState>(createInitialState)
  const [touched, setTouched] = useState(false)

  const handleChange = (event: ChangeEvent<HTMLInputElement>) => {
    const { name, value } = event.target
    setFormState((prev) => ({ ...prev, [name]: value }))
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setTouched(true)

    if (!formState.name.trim() || !formState.serial.trim() || !formState.purchaseDate) {
      return
    }

    const payload: CreateProductPayload = {
      name: formState.name.trim(),
      serial: formState.serial.trim(),
      purchaseDate: formState.purchaseDate,
    }

    if (formState.warrantyMonths.trim().length > 0) {
      const warrantyMonthsValue = Number(formState.warrantyMonths)
      if (!Number.isNaN(warrantyMonthsValue)) {
        payload.warrantyMonths = warrantyMonthsValue
      }
    }

    if (formState.price.trim().length > 0) {
      const priceValue = Number(formState.price)
      if (!Number.isNaN(priceValue)) {
        payload.price = priceValue
      }
    }

    if (formState.brand.trim()) {
      payload.brand = formState.brand.trim()
    }

    if (formState.retailer.trim()) {
      payload.retailer = formState.retailer.trim()
    }

    const saved = await onCreate(payload)
    if (saved) {
      setTouched(false)
      setFormState(createInitialState())
    }
  }

  const showValidationWarning =
    touched && (!formState.name.trim() || !formState.serial.trim() || !formState.purchaseDate)

  return (
    <form className="product-form" onSubmit={handleSubmit}>
      <div className="form-grid">
        <label>
          <span>Name *</span>
          <input
            type="text"
            name="name"
            value={formState.name}
            onChange={handleChange}
            placeholder="Phone, laptop, appliance..."
            required
          />
        </label>
        <label>
          <span>Serial *</span>
          <input
            type="text"
            name="serial"
            value={formState.serial}
            onChange={handleChange}
            placeholder="SN12345"
            required
          />
        </label>
        <label>
          <span>Purchase date *</span>
          <input type="date" name="purchaseDate" value={formState.purchaseDate} onChange={handleChange} required />
        </label>
        <label>
          <span>Warranty months</span>
          <input type="number" min={1} name="warrantyMonths" value={formState.warrantyMonths} onChange={handleChange} />
        </label>
        <label>
          <span>Brand</span>
          <input type="text" name="brand" value={formState.brand} onChange={handleChange} placeholder="Acme" />
        </label>
        <label>
          <span>Retailer</span>
          <input type="text" name="retailer" value={formState.retailer} onChange={handleChange} placeholder="Store" />
        </label>
        <label>
          <span>Price (optional)</span>
          <input type="number" min={0} step="0.01" name="price" value={formState.price} onChange={handleChange} placeholder="0.00" />
        </label>
      </div>

      {showValidationWarning && <p className="form-hint warning">Name, serial, and purchase date are required.</p>}
      {serverError && <p className="form-hint error">{serverError}</p>}
      {successMessage && <p className="form-hint success">{successMessage}</p>}

      <div className="form-actions">
        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving...' : 'Save product'}
        </button>
      </div>
    </form>
  )
}

export default ProductForm
