import { useEffect } from 'react'
import { useDispatch, useSelector } from 'react-redux'
import EmptyState from '../components/EmptyState'
import ErrorState from '../components/ErrorState'
import Loader from '../components/Loader'
import {
  fetchPosts,
  selectPosts,
  selectPostsError,
  selectPostsStatus,
} from '../features/posts/postsSlice'

// Faz 0: tasarim yok. Amaci mimarinin uctan uca calistigini gostermek.
function FeedPage() {
  const dispatch = useDispatch()
  const posts = useSelector(selectPosts)
  const status = useSelector(selectPostsStatus)
  const error = useSelector(selectPostsError)

  useEffect(() => {
    dispatch(fetchPosts({ page: 1, size: 20 }))
  }, [dispatch])

  if (status === 'idle' || status === 'loading') {
    return <Loader label="Akis yukleniyor..." />
  }

  if (status === 'failed') {
    return <ErrorState error={error} onRetry={() => dispatch(fetchPosts({ page: 1, size: 20 }))} />
  }

  if (posts.length === 0) {
    return <EmptyState title="Akis bos" description="Henuz hic post paylasilmamis." />
  }

  return (
    <ul>
      {posts.map((post) => (
        <li key={post.id}>
          <strong>{post.userName}</strong> — {post.content}
        </li>
      ))}
    </ul>
  )
}

export default FeedPage
