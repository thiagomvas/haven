import { useTranslation } from 'react-i18next';

import type { DockerfileSource } from '@/api/types/service.types';

import { useBranchAutocomplete } from '../../hooks/useBranchAutocomplete';
import { BranchInput } from '../ui/BranchInput';
import { FormGroup, FormInput, FormLabel, FormTextarea } from '../ui/Form';
import { SelectInput } from '../ui/SelectInput';
import styles from '@/styles/components/services/DockerfileConfigFields.module.css';

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
  disabled,
}: DockerfileConfigFieldsProps) {
  const { t } = useTranslation('services');

  const { branches, isLoading: branchesLoading } = useBranchAutocomplete(
    source === 'Git' ? repository : '',
    gitCredentialId
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
            <FormLabel htmlFor="repository" required>
              {t('createPage.repositoryUrl')}
            </FormLabel>
            <FormInput
              id="repository"
              type="url"
              placeholder={t('createPage.repositoryUrlPlaceholder')}
              value={repository}
              onChange={e => onRepositoryChange(e.target.value)}
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
