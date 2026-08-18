import axios from 'axios'
import { apiErrorFromEnvelope, toApiError } from './apiError'

const baseURL = import.meta.env.VITE_API_URL

if (!baseURL) {
  throw new Error('VITE_API_URL tanimli degil. .env.example dosyasini .env.development olarak kopyalayin.')
}

const http = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
})

// Basarili cevaplarda ApiResponse zarfini soyar; cagiran taraf dogrudan
// data'yi alir. Hatalarda ise her zaman ayni sekle sahip bir nesne firlatir.
http.interceptors.response.use(
  (response) => {
    const envelope = response.data

    if (!envelope || typeof envelope !== 'object' || !('isSuccess' in envelope)) {
      return envelope
    }

    if (!envelope.isSuccess) {
      return Promise.reject(apiErrorFromEnvelope(envelope, response.status))
    }

    return envelope.data
  },
  (error) => Promise.reject(toApiError(error)),
)

export default http
