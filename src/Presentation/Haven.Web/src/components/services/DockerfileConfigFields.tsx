import { useTranslation } from 'react-i18next';

import type { DockerfileSource, RestartPolicy } from '@/api/types';
import styles from '@/styles/components/services/DockerfileConfigFields.module.css';

import { useBranchAutocomplete } from '../../hooks/useBranchAutocomplete';
import { useRepositoryAutocomplete } from '../../hooks/useRepositoryAutocomplete';
import { BranchInput } from '../ui/BranchInput';
import { FormGroup, FormInput, FormLabel, FormTextarea } from '../ui/Form';
import { RepositoryInput } from '../ui/RepositoryInput';
import { SelectInput } from '../ui/SelectInput';

const RESTART_POLICIES: RestartPolicy[] = ['No', 'Always', 'UnlessStopped', 'OnFailure'];

interface Credential {
  id: string;
  displayName: string;
}

interface DockerfileConfigFieldsProps {
  source: DockerfileSource;
  onSourceChange: (source: DockerfileSource) => void;
  repository: string;
  onRepositoryChange: (value: string) => void;
  branch: string;
  onBranchChange: (value: string) => void;
  filePath: string;
  onFilePathChange: (value: string) => void;
  rawContent: string;
  onRawContentChange: (value: string) => void;
  gitCredentialId: string | undefined;
  onGitCredentialIdChange: (value: string | undefined) => void;
  credentials: Credential[];
  restartPolicy: RestartPolicy;
  onRestartPolicyChange: (policy: RestartPolicy) => void;
  disabled?: boolean;
}

export function DockerfileConfigFields({
  source,
  onSourceChange,
  repository,
  onRepositoryChange,
  branch,
  onBranchChange,
  filePath,
  onFilePathChange,
  rawContent,
  onRawContentChange,
  gitCredentialId,
  onGitCredentialIdChange,
  credentials,
  restartPolicy,
  onRestartPolicyChange,
  disabled,
}: DockerfileConfigFieldsProps) {
  const { t } = useTranslation('services');

  const { branches, isLoading: branchesLoading } = useBranchAutocomplete(
    source === 'Git' ? repository : '',
    gitCredentialId
  );

  const { repositories, isLoading: repositoriesLoading } = useRepositoryAutocomplete(
    source === 'Git' ? gitCredentialId : undefined
  );

  return (
    <div className={styles.configFields}>
      <h3 className={styles.configTitle}>{t('createPage.dockerfileConfiguration')}</h3>

      <FormGroup>
        <FormLabel htmlFor="source">{t('createPage.source')}</FormLabel>
        <div className={styles.sourceToggle}>
          <button
            type="button"
            className={`${styles.toggleButton} ${source === 'Git' ? styles.active : ''}`}
            onClick={() => onSourceChange('Git')}
            disabled={disabled}
          >
            {t('createPage.gitRepository')}
          </button>
          <button
            type="button"
            className={`${styles.toggleButton} ${source === 'Raw' ? styles.active : ''}`}
            onClick={() => onSourceChange('Raw')}
            disabled={disabled}
          >
            {t('createPage.rawContent')}
          </button>
        </div>
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

      {source === 'Git' ? (
        <>
          <FormGroup>
            <SelectInput
              label={t('createPage.gitCredential')}
              value={gitCredentialId ?? ''}
              onChange={v => onGitCredentialIdChange(v || undefined)}
              options={credentials.map(c => ({ value: c.id, label: c.displayName }))}
              placeholder={t('createPage.gitCredentialPlaceholder')}
              disabled={disabled}
            />
          </FormGroup>
          <FormGroup>
            <RepositoryInput
              id="repository"
              label={`${t('createPage.repositoryUrl')} ${t('createPage.required')}`}
              placeholder={t('createPage.repositoryUrlPlaceholder')}
              value={repository}
              onChange={onRepositoryChange}
              repositories={repositories}
              isLoadingRepositories={repositoriesLoading}
              disabled={disabled}
            />
          </FormGroup>
          <FormGroup>
            <BranchInput
              label={`${t('createPage.branch')} ${t('createPage.required')}`}
              value={branch}
              onChange={onBranchChange}
              branches={branches}
              isLoadingBranches={branchesLoading}
              disabled={disabled}
            />
          </FormGroup>
          <FormGroup>
            <div className={styles.labelWithHelp}>
              <FormLabel htmlFor="filePath">{t('createPage.dockerfilePath')}</FormLabel>
              <span className={styles.helpText}>{t('createPage.dockerfilePathHelp')}</span>
            </div>
            <FormInput
              id="filePath"
              type="text"
              placeholder={t('createPage.dockerfilePathPlaceholder')}
              value={filePath}
              onChange={e => onFilePathChange(e.target.value)}
              disabled={disabled}
            />
          </FormGroup>
        </>
      ) : (
        <FormGroup>
          <FormLabel htmlFor="rawContent" required>
            {t('createPage.dockerfileContent')}
          </FormLabel>
          <FormTextarea
            id="rawContent"
            className={styles.dockerfileTextarea}
            placeholder={t('createPage.dockerfileContentPlaceholder')}
            value={rawContent}
            onChange={e => onRawContentChange(e.target.value)}
            disabled={disabled}
          />
        </FormGroup>
      )}
    </div>
  );
}
