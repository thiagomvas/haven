import { ServiceDto, ServiceStatus } from '../../api/types'
import { Card, CardContent, CardHeader } from '../ui/Card'
import styles from './ServiceCard.module.css'

interface ServiceCardProps {
  service: ServiceDto
  onClick?: () => void
}

function getStatusColor(status: ServiceStatus): string {
  switch (status) {
    case 'Running':
      return styles.statusRunning
    case 'Stopped':
      return styles.statusStopped
    case 'Degraded':
      return styles.statusDegraded
    default:
      return styles.statusUnknown
  }
}

export function ServiceCard({ service, onClick }: ServiceCardProps) {
  return (
    <Card
      className={styles.serviceCard}
      onClick={() => onClick?.()}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          onClick?.()
        }
      }}
    >
      <CardHeader>
        <div className={styles.header}>
          <div>
            <h4 className={styles.title}>{service.name}</h4>
            <p className={styles.type}>{service.type}</p>
          </div>
          <div className={`${styles.status} ${getStatusColor(service.status)}`}>
            {service.status}
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <div className={styles.meta}>
          <div className={styles.metaItem}>
            <span className={styles.label}>Exposure</span>
            <span className={styles.value}>{service.exposureMode}</span>
          </div>
          <div className={styles.metaItem}>
            <span className={styles.label}>Updated</span>
            <span className={styles.value}>
              {new Date(service.updatedAt).toLocaleDateString()}
            </span>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
