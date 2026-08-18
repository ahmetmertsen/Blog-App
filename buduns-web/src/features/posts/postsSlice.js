import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { ApiErrorCodes, messageForCode } from '../../lib/apiError'
import { getAllPosts } from './postsApi'

export const fetchPosts = createAsyncThunk(
  'posts/fetchPosts',
  async ({ page = 1, size = 20 } = {}, { rejectWithValue, signal }) => {
    try {
      return await getAllPosts({ page, size, signal })
    } catch (error) {
      return rejectWithValue(error)
    }
  },
)

const initialState = {
  items: [],
  status: 'idle',
  error: null,
  page: 1,
  totalPages: 0,
  hasMore: false,
}

const postsSlice = createSlice({
  name: 'posts',
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchPosts.pending, (state) => {
        state.status = 'loading'
        state.error = null
      })
      .addCase(fetchPosts.fulfilled, (state, action) => {
        const { items, page, totalPages } = action.payload
        state.status = 'succeeded'
        state.items = items
        state.page = page
        state.totalPages = totalPages
        state.hasMore = page < totalPages
      })
      .addCase(fetchPosts.rejected, (state, action) => {
        // Iptal edilen istek hata degildir; kullaniciya gosterilmez.
        if (action.payload?.code === ApiErrorCodes.Canceled) return

        state.status = 'failed'
        state.error = action.payload ?? {
          code: ApiErrorCodes.Unknown,
          message: messageForCode(ApiErrorCodes.Unknown),
          validationErrors: null,
          traceId: null,
          status: null,
        }
      })
  },
})

export const selectPosts = (state) => state.posts.items
export const selectPostsStatus = (state) => state.posts.status
export const selectPostsError = (state) => state.posts.error

export default postsSlice.reducer
