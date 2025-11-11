import type { Product } from '../api'

interface ProductListProps {
  products: Product[]
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

const ProductList = ({ products }: ProductListProps) => {
  return (
    <section className="card">
      <div className="card-header">
        <div>
          <h2>Products</h2>
          <p className="muted">{products.length === 0 ? 'No tracked purchases yet.' : `Showing ${products.length} item(s).`}</p>
        </div>
      </div>

      {products.length === 0 ? (
        <p className="muted">Use the form above to add your first purchase - data stays in your local SQLite DB.</p>
      ) : (
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Serial</th>
                <th>Brand</th>
                <th>Purchase date</th>
                <th>Warranty (months)</th>
                <th>Retailer</th>
                <th>Price</th>
              </tr>
            </thead>
            <tbody>
              {products.map((product) => (
                <tr key={product.id}>
                  <td>
                    <span className="table-title">{product.name}</span>
                  </td>
                  <td>{product.serial}</td>
                  <td>{product.brand ?? '--'}</td>
                  <td>{formatDate(product.purchaseDate)}</td>
                  <td>{product.warrantyMonths}</td>
                  <td>{product.retailer ?? '--'}</td>
                  <td>{formatPrice(product.price)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

export default ProductList
