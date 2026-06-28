import { ServiceDto } from '@/api/types/service.types';
import { DockerConfig } from '@/api/types/service.types';
import { ServiceStatus } from '@/api/types/service.types';
import { Row, Spacer } from '../layout';
import { Card, CardContent, CardHeader } from '../ui/Card';
import { ServiceExposureChip } from '../ui/chips/serviceExposureChip';
import { ServiceTypeChip } from '../ui/chips/serviceTypeChip';
import { HealthIndicator } from '../ui/HealthIndicator';
import { Label } from '../ui/Label';
import styles from './ServiceCard.module.css';

interface ServiceCardProps {
  service: ServiceDto;
  onClick?: () => void;
}

function getStatusColor(status: ServiceStatus): string {
  switch (status) {
    case 'Running':
      return styles.statusRunning;
    case 'Stopped':
      return styles.statusStopped;
    case 'Degraded':
      return styles.statusDegraded;
    case 'DeploymentPending':
      return styles.statusDeploymentPending;
    default:
      return styles.statusUnknown;
  }
}

function DockerImageContent({ service }: { service: ServiceDto }) {
  const config = service.sourceConfig as DockerConfig | undefined;
  if (!config) return null;

  return (
    <Row gap="2">
      <Label variant="secondary" size="sm">
        Image:
      </Label>
      <code className={styles.inlineCode}>{config.image}</code>
    </Row>
  );
}

function DockerfileContent({ service }: { service: ServiceDto }) {
  const config = service.sourceConfig as any;
  if (!config) return null;

  const isGitSource = config.source === 'Git';
  const repoName = isGitSource && config.repository ? config.repository.split('/').pop() : null;

  return (
    <Row gap="2">
      <Label variant="secondary" size="sm">
        Source:
      </Label>
      <span className={styles.sourceValue}>
        {isGitSource ? `Git${repoName ? ` • ${repoName}` : ''}` : 'Raw'}
      </span>
    </Row>
  );
}

function ComposeContent() {
  return <div>Compose content goes here</div>;
}

function ProcessContent() {
  return <div>Process content goes here</div>;
}

export function ServiceCard({ service, onClick }: ServiceCardProps) {
  return (
    <Card
      className={styles.serviceCard}
      onClick={() => onClick?.()}
      role="button"
      tabIndex={0}
      onKeyDown={e => {
        if (e.key === 'Enter' || e.key === ' ') {
          onClick?.();
        }
      }}
    >
      <CardHeader>
        <div>
          <Row gap="2" full>
            <HealthIndicator health={service.status.toLocaleLowerCase()} />
            <Label variant="primary" size="xl" weight="bold">
              {service.name}
            </Label>
            <Spacer expand direction="horizontal" />
            <ServiceTypeChip serviceType={service.type} />
          </Row>
          <Spacer size="4" />
          <Row gap="2" full>
            <ServiceExposureChip exposureMode={service.exposureMode} />
          </Row>
        </div>
      </CardHeader>
      <CardContent>
        {service.type === 'DockerImage' && <DockerImageContent service={service} />}
        {service.type === 'Dockerfile' && <DockerfileContent service={service} />}
        {service.type === 'Compose' && <ComposeContent />}
        {service.type === 'Process' && <ProcessContent />}
      </CardContent>
    </Card>
  );
}
