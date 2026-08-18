import Box from '@mui/material/Box'
import Typography from '@mui/material/Typography'

function EmptyState({ title = 'Gosterilecek bir sey yok', description }) {
  return (
    <Box sx={{ textAlign: 'center', py: 6 }}>
      <Typography variant="h6" color="text.secondary">{title}</Typography>
      {description && (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
          {description}
        </Typography>
      )}
    </Box>
  )
}

export default EmptyState
