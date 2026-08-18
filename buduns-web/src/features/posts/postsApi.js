import http from '../../lib/http'

export function getAllPosts({ page = 1, size = 20, signal } = {}) {
  return http.get('/Post/getAll', { params: { page, size }, signal })
}
