import Button from '@mui/material/Button'
import { Link as RouterLink } from 'react-router'
import EmptyState from '../components/EmptyState'

function NotFoundPage() {
  return (
    <>
      <EmptyState title="404" description="Aradiginiz sayfa bulunamadi." />
      <Button component={RouterLink} to="/" variant="contained" sx={{ display: 'block', mx: 'auto', width: 'fit-content' }}>
        Akisa don
      </Button>
    </>
  )
}

export default NotFoundPage
