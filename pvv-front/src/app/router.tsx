import { createBrowserRouter } from 'react-router-dom'
import { PurchasePage } from '../pages/purchase/PurchasePage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <PurchasePage />,
  },
])
