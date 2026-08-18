import Box from '@mui/material/Box'
import CircularProgress from '@mui/material/CircularProgress'

function Loader({ label = 'Yukleniyor...' }) {
  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2, py: 6 }}>
      <CircularProgress />
      <Box component="span" sx={{ color: 'text.secondary' }}>{label}</Box>
    </Box>
  )
}

export default Loader
