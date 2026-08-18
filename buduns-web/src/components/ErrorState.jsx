import Alert from '@mui/material/Alert'
import AlertTitle from '@mui/material/AlertTitle'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Typography from '@mui/material/Typography'

function ErrorState({ error, onRetry }) {
  if (!error) return null

  return (
    <Box sx={{ py: 4 }}>
      <Alert
        severity="error"
        action={onRetry && (
          <Button color="inherit" size="small" onClick={onRetry}>Tekrar dene</Button>
        )}
      >
        <AlertTitle>Bir hata olustu</AlertTitle>
        {error.message}
        {error.traceId && (
          <Typography variant="caption" component="div" sx={{ mt: 1, opacity: 0.7 }}>
            {error.code} · {error.traceId}
          </Typography>
        )}
      </Alert>
    </Box>
  )
}

export default ErrorState
