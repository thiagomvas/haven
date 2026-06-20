import { useTranslation } from 'react-i18next';
import type { RestartPolicy } from '../../api/types';
import { SelectInput } from '../ui/SelectInput';
import { FormGroup, FormLabel, FormInput } from '../ui/Form';
import styles from './DockerImageConfigFields.module.css';

const RESTART_POLICIES: RestartPolicy[] = ['No', 'Always', 'UnlessStopped', 'OnFailure'];

interface DockerImageConfigFieldsProps {
  dockerImage: string;
  onDockerImageChange: (value: string) => void;
  restartPolicy: RestartPolicy;
  onRestartPolicyChange: (policy: RestartPolicy) => void;
  disabled?: boolean;
}

export function DockerImageConfigFields({
  dockerImage,
  onDockerImageChange,
  restartPolicy,
  onRestartPolicyChange,
  disabled,
}: DockerImageConfigFieldsProps) {
  const { t } = useTranslation('services');

  return (
    <div className={styles.configFields}>
      <h3 className={styles.configTitle}>{t('createPage.dockerImageConfiguration')}</h3>
      <FormGroup>
        <FormLabel htmlFor="dockerImage" required>
          {t('createPage.dockerImageLabel')}
        </FormLabel>
        <FormInput
          id="dockerImage"
          type="text"
          placeholder={t('createPage.dockerImagePlaceholder')}
          value={dockerImage}
          onChange={e => onDockerImageChange(e.target.value)}
          disabled={disabled}
        />
      </FormGroup>
      <FormGroup>
        <SelectInput
          label={t('createPage.restartPolicy')}
          value={restartPolicy}
          onChange={v => onRestartPolicyChange(v as RestartPolicy)}
          options={RESTART_POLICIES.map(p => ({ value: p, label: p }))}
          disabled={disabled}
        />
      </FormGroup>
    </div>
  );
}
