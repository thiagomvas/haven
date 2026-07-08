import { Check } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useSearchParams } from 'react-router-dom';

import { CreateEnvironmentInput } from '@/api/types/environment.types';
import { ProjectDto } from '@/api/types/project.types';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import styles from '@/styles/components/environments/CreateEnvironmentPage.module.css';

import { environmentsApi } from '../../api/environments';
import { projectsApi } from '../../api/projects';
import { Banner } from '../ui/Banner';
import { Button } from '../ui/Button';
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '../ui/Card';
import { FormGroup, FormInput, FormLabel, FormSelect, FormTextarea } from '../ui/Form';

export function CreateEnvironmentPage() {
  const { t } = useTranslation('environments');
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const projectIdParam = searchParams.get('projectId');

  useSetBreadcrumbs([{ label: 'Projects', to: '/projects' }, { label: 'Create Environment' }]);

  // Form state
  const [name, setName] = useState('');
  const [alias, setAlias] = useState('');
  const [description, setDescription] = useState('');
  const [envVarsText, setEnvVarsText] = useState('');

  // UI state
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [status, setStatus] = useState<'idle' | 'creating' | 'success' | 'error'>('idle');
  const [selectedProjectId, setSelectedProjectId] = useState(projectIdParam || '');
  const [projects, setProjects] = useState<ProjectDto[]>([]);
  const [projectsLoading, setProjectsLoading] = useState(true);
  const [createdEnvironmentId, setCreatedEnvironmentId] = useState<string | null>(null);

  // Load projects on mount
  useEffect(() => {
    const loadProjects = async () => {
      try {
        setProjectsLoading(true);
        const result = await projectsApi.getAll({ pageNumber: 1, pageSize: 100 });
        setProjects(result.items);
      } catch (err) {
        console.error('Failed to load projects', err);
      } finally {
        setProjectsLoading(false);
      }
    };

    loadProjects();
  }, []);

  const isFormValid = () => {
    return selectedProjectId && name.trim().length > 0;
  };

  const handleSubmit = async () => {
    setError(null);

    if (!isFormValid()) {
      setError(t('createPage.fillRequiredFields', 'Please fill in all required fields'));
      return;
    }

    if (!selectedProjectId) {
      setError(t('createPage.projectRequired', 'Please select a project'));
      return;
    }

    const input: CreateEnvironmentInput = {
      name: name.trim(),
      alias: alias.trim() || undefined,
      description: description.trim() || undefined,
    };

    setIsLoading(true);
    setStatus('creating');
    try {
      const environmentId = await environmentsApi.create(selectedProjectId, input);
      setCreatedEnvironmentId(environmentId);

      if (envVarsText.trim()) {
        await environmentsApi.setEnvironmentVariables(
          selectedProjectId,
          environmentId,
          envVarsText
        );
      }

      setStatus('success');
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : t('createPage.failedToCreate', 'Failed to create environment')
      );
      setStatus('error');
    } finally {
      setIsLoading(false);
    }
  };

  const handleViewEnvironment = () => {
    if (selectedProjectId && createdEnvironmentId) {
      navigate(`/projects/${selectedProjectId}/environments/${createdEnvironmentId}`);
    }
  };

  const handleViewProject = () => {
    if (selectedProjectId) {
      navigate(`/projects/${selectedProjectId}`);
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h1>{t('createPage.title', 'Create Environment')}</h1>
        <p>{t('createPage.description', 'Add a new environment to your project')}</p>
      </div>

      <div className={styles.content}>
        {status === 'creating' && (
          <Banner variant="info" title={t('createPage.creating', 'Creating environment...')} />
        )}
        {status === 'success' && (
          <Banner
            variant="success"
            title={t('createPage.createdSuccessfully', 'Environment created successfully')}
          />
        )}
        {error && <Banner variant="error" description={error} />}

        {status === 'success' ? (
          <Card className={styles.successCard}>
            <CardHeader>
              <CardTitle>{t('createPage.successTitle', 'Environment Created')}</CardTitle>
              <p className={styles.cardDescription}>
                {t('createPage.successDescription', 'Your environment is ready to use')}
              </p>
            </CardHeader>

            <div className={styles.successContent}>
              <div className={styles.successIcon}>
                <Check size={40} />
              </div>
              <p
                className={styles.successMessage}
                dangerouslySetInnerHTML={{
                  __html: t(
                    'createPage.successMessage',
                    'Environment {{name}} has been created successfully'
                  ).replace('{{name}}', `<strong>${name}</strong>`),
                }}
              />
            </div>

            <CardFooter>
              <Button variant="secondary" onClick={handleViewProject}>
                {t('createPage.viewProject', 'View Project')}
              </Button>
              <Button variant="primary" onClick={handleViewEnvironment}>
                {t('createPage.viewEnvironment', 'View Environment')}
              </Button>
            </CardFooter>
          </Card>
        ) : (
          <>
            <Card>
              <CardHeader>
                <CardTitle>{t('createPage.environmentDetails', 'Environment Details')}</CardTitle>
                <p className={styles.cardDescription}>
                  {t(
                    'createPage.environmentDetailsDescription',
                    'Enter the information for your environment'
                  )}
                </p>
              </CardHeader>

              <CardContent>
                <div className={styles.formSection}>
                  <FormGroup>
                    <FormLabel htmlFor="project" required>
                      {t('createPage.project', 'Project')}
                    </FormLabel>
                    <FormSelect
                      id="project"
                      value={selectedProjectId}
                      onChange={e => setSelectedProjectId(e.target.value)}
                      disabled={isLoading || projectsLoading || !!projectIdParam}
                      style={{ backgroundColor: 'var(--color-surface-2)' }}
                    >
                      <option value="">
                        {t('createPage.projectPlaceholder', 'Select a project')}
                      </option>
                      {projects.map(p => (
                        <option key={p.id} value={p.id}>
                          {p.name}
                        </option>
                      ))}
                    </FormSelect>
                  </FormGroup>

                  <FormGroup>
                    <FormLabel htmlFor="envName" required>
                      {t('createPage.environmentName', 'Environment Name')}
                    </FormLabel>
                    <FormInput
                      id="envName"
                      type="text"
                      placeholder={t(
                        'createPage.environmentNamePlaceholder',
                        'e.g., Development, Staging, Production'
                      )}
                      value={name}
                      onChange={e => setName(e.target.value)}
                      disabled={isLoading}
                      maxLength={64}
                      style={{ backgroundColor: 'var(--color-surface-2)' }}
                    />
                  </FormGroup>

                  <FormGroup>
                    <FormLabel htmlFor="envAlias">
                      Alias{' '}
                      <span
                        style={{
                          fontSize: 'var(--text-xs)',
                          color: 'var(--color-text-secondary)',
                          fontWeight: 'normal',
                        }}
                      >
                        — used in Docker names, e.g. <code>haven-...-dev</code> (2–8 chars)
                      </span>
                    </FormLabel>
                    <FormInput
                      id="envAlias"
                      type="text"
                      placeholder="e.g., dev, prod, stg"
                      value={alias}
                      onChange={e => setAlias(e.target.value.toLowerCase())}
                      disabled={isLoading}
                      maxLength={8}
                      style={{ backgroundColor: 'var(--color-surface-2)' }}
                    />
                  </FormGroup>

                  <FormGroup>
                    <FormLabel htmlFor="envDescription">
                      {t('createPage.description', 'Description')}
                    </FormLabel>
                    <FormTextarea
                      id="envDescription"
                      placeholder={t(
                        'createPage.descriptionPlaceholder',
                        'Describe this environment...'
                      )}
                      value={description}
                      onChange={e => setDescription(e.target.value)}
                      disabled={isLoading}
                      maxLength={250}
                      rows={4}
                      style={{ backgroundColor: 'var(--color-surface-2)' }}
                    />
                    <span className={styles.charCount}>{description.length}/250</span>
                  </FormGroup>
                </div>
              </CardContent>
            </Card>

            {/* Card 2: Environment Variables */}
            <Card>
              <CardHeader>
                <CardTitle>{t('variablesPage.title', 'Environment Variables')}</CardTitle>
                <p className={styles.cardDescription}>
                  {t(
                    'variablesPage.description',
                    'Set environment-scoped variables available to all services in this environment'
                  )}
                </p>
              </CardHeader>

              <CardContent>
                <div className={styles.formSection}>
                  <FormGroup>
                    <div className={styles.labelWithHelp}>
                      <FormLabel htmlFor="envVars">
                        {t('variablesPage.variables', 'Variables')}
                      </FormLabel>
                      <span className={styles.helpText}>
                        {t('variablesPage.help', 'One variable per line in KEY=VALUE format')}
                      </span>
                    </div>
                    <FormTextarea
                      id="envVars"
                      placeholder={t(
                        'variablesPage.placeholder',
                        'DATABASE_URL=postgres://localhost\nAPI_KEY=your-secret-key'
                      )}
                      value={envVarsText}
                      onChange={e => setEnvVarsText(e.target.value)}
                      disabled={isLoading}
                      rows={8}
                      style={{ backgroundColor: 'var(--color-surface-2)' }}
                    />
                  </FormGroup>
                </div>
              </CardContent>
            </Card>
          </>
        )}

        {status !== 'success' && (
          <div className={styles.submitSection}>
            <Button variant="secondary" onClick={() => navigate(-1)} disabled={isLoading}>
              {t('createPage.cancel', 'Cancel')}
            </Button>
            <Button
              variant="primary"
              onClick={handleSubmit}
              isLoading={isLoading}
              disabled={!isFormValid()}
            >
              {t('createPage.createButton', 'Create Environment')}
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}
