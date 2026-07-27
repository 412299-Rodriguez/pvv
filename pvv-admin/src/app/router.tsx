import { createBrowserRouter, Navigate } from 'react-router-dom'

import { LoginPage } from '@/pages/login/LoginPage'
import { DashboardPage } from '@/pages/dashboard/DashboardPage'
import { LeadsPage } from '@/pages/leads/LeadsPage'
import { AppearancePage } from '@/pages/appearance/AppearancePage'
import { ProductsPage } from '@/pages/products/ProductsPage'
import { CompaniesPage } from '@/pages/companies/CompaniesPage'
import { OperatorsPage } from '@/pages/operators/OperatorsPage'

import { ProtectedLayout, HomeRedirect, RoleRoute } from './guards'

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  {
    element: <ProtectedLayout />,
    children: [
      { index: true, element: <HomeRedirect /> },
      // CompanyOperator
      {
        path: 'dashboard',
        element: (
          <RoleRoute allow="CompanyOperator">
            <DashboardPage />
          </RoleRoute>
        ),
      },
      {
        path: 'leads',
        element: (
          <RoleRoute allow="CompanyOperator">
            <LeadsPage />
          </RoleRoute>
        ),
      },
      {
        path: 'apariencia',
        element: (
          <RoleRoute allow="CompanyOperator">
            <AppearancePage />
          </RoleRoute>
        ),
      },
      {
        path: 'productos',
        element: (
          <RoleRoute allow="CompanyOperator">
            <ProductsPage />
          </RoleRoute>
        ),
      },
      // SystemAdmin
      {
        path: 'companias',
        element: (
          <RoleRoute allow="SystemAdmin">
            <CompaniesPage />
          </RoleRoute>
        ),
      },
      {
        path: 'operadores',
        element: (
          <RoleRoute allow="SystemAdmin">
            <OperatorsPage />
          </RoleRoute>
        ),
      },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
])
