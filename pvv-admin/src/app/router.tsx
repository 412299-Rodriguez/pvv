import { createBrowserRouter } from 'react-router-dom'
import { DashboardPage } from '../pages/dashboard/DashboardPage'
import { CompaniesPage } from '../pages/companies/CompaniesPage'
import { ProductsPage } from '../pages/products/ProductsPage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <DashboardPage />,
  },
  {
    path: '/companies',
    element: <CompaniesPage />,
  },
  {
    path: '/products',
    element: <ProductsPage />,
  },
])
