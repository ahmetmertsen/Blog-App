import { createBrowserRouter } from 'react-router'
import MainLayout from '../layouts/MainLayout'
import FeedPage from '../pages/FeedPage'
import NotFoundPage from '../pages/NotFoundPage'

const router = createBrowserRouter([
  {
    path: '/',
    element: <MainLayout />,
    children: [
      { index: true, element: <FeedPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])

export default router
