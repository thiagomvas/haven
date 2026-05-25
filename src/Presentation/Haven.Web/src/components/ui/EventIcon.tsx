import { Rocket, CheckCircle2, AlertTriangle, AlertCircle, Plus, Edit2, Trash2, LucideIcon } from 'lucide-react'
import { clsx } from 'clsx'
import { Badge } from './Badge'
import styles from './EventIcon.module.css'

export const EVENT_TYPES = {
    EnvironmentCreated: 'EnvironmentCreatedEvent',
    EnvironmentVariablesUpdated: 'EnvironmentVariablesUpdatedEvent',
    ProjectCreated: 'ProjectCreatedEvent',
    ServiceCreated: 'ServiceCreatedEvent',
    ServiceDeployed: 'ServiceDeployedEvent',
    ServiceUpdated: 'ServiceUpdatedEvent',
    ServiceStopped: 'ServiceStoppedEvent',
} as const

type EventType = typeof EVENT_TYPES[keyof typeof EVENT_TYPES]

interface EventConfig {
  icon: LucideIcon
  variant: 'primary' | 'success' | 'warning' | 'danger' | 'default'
  label: string
}

const EVENT_CONFIG: Record<EventType, EventConfig> = {
    [EVENT_TYPES.EnvironmentCreated]: {
        icon: Plus,
        variant: 'success',
        label: 'Environment Created',
    },
    [EVENT_TYPES.EnvironmentVariablesUpdated]: {
        icon: Edit2,
        variant: 'warning',
        label: 'Environment Variables Updated',
    },
    [EVENT_TYPES.ProjectCreated]: {
        icon: Plus,
        variant: 'primary',
        label: 'Project Created',
    },
    [EVENT_TYPES.ServiceCreated]: {
        icon: Plus,
        variant: 'primary',
        label: 'Service Created',
    },
    [EVENT_TYPES.ServiceDeployed]: {
        icon: Rocket,
        variant: 'success',
        label: 'Service Deployed',
    },
    [EVENT_TYPES.ServiceUpdated]: {
        icon: Edit2,
        variant: 'warning',
        label: 'Service Updated',
    },
    [EVENT_TYPES.ServiceStopped]: {
        icon: AlertTriangle,
        variant: 'danger',
        label: 'Service Stopped',
    },  
}

interface EventIconProps {
  type: string
  className?: string
}

export function EventIcon({ type, className }: EventIconProps) {
  const config = EVENT_CONFIG[type as EventType]

  if (!config) {
    return (
      <Badge variant="default" className={clsx(styles.badge, className)}>
        <AlertCircle size={16} />
      </Badge>
    )
  }

  const IconComponent = config.icon

  return (
    <Badge variant={config.variant} className={clsx(styles.badge, className)} title={config.label}>
      <IconComponent size={16} />
    </Badge>
  )
}
