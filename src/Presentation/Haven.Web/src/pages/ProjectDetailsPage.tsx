import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { Plus } from "lucide-react";
import { projectsApi } from "../api/projects";
import { environmentsApi } from "../api/environments";
import { ProjectDto, EnvironmentDto } from "../api/types";
import { EnvironmentCard } from "../components/projects/EnvironmentCard";
import { CreateEnvironmentModal } from "../components/projects/CreateEnvironmentModal";
import { EnvironmentVariablesEditor } from "../components/projects/EnvironmentVariablesEditor";
import { ProjectSettingsForm } from "../components/projects/ProjectSettingsForm";
import { Button } from "../components/ui/Button";
import { Spinner } from "../components/ui/Spinner";
import styles from "./ProjectDetailsPage.module.css";
import { ProjectAvatar } from "@/components/ui/ProjectAvatar";
import { Row, ConfigurationPageLayout, Stack, Grid, Spacer } from "@/components/layout";

export function ProjectDetailsPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation("projects");

  const [project, setProject] = useState<ProjectDto | null>(null);
  const [environments, setEnvironments] = useState<EnvironmentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isCreateEnvModalOpen, setIsCreateEnvModalOpen] = useState(false);
  const [editingEnvironment, setEditingEnvironment] =
    useState<EnvironmentDto | null>(null);
  const [isConfigOpen, setIsConfigOpen] = useState(false);

  useEffect(() => {
    const loadProjectData = async () => {
      if (!projectId) return;

      try {
        setLoading(true);
        setError(null);

        const [projectData, environmentsData] = await Promise.all([
          projectsApi.getById(projectId),
          environmentsApi.getByProjectId(projectId),
        ]);

        if (!projectData) {
          setError("Project not found");
          return;
        }

        setProject(projectData);
        setEnvironments(environmentsData || []);
      } catch (err) {
        setError(err instanceof Error ? err.message : t("error"));
      } finally {
        setLoading(false);
      }
    };

    loadProjectData();
  }, [projectId, t]);

  const handleCreateEnvironmentSuccess = async () => {
    if (!projectId) return;
    try {
      const environmentsData = await environmentsApi.getByProjectId(projectId);
      setEnvironments(environmentsData || []);
    } catch (err) {
      console.error("Failed to refresh environments", err);
    }
  };

  const handleProjectUpdated = async () => {
    if (!projectId) return;
    try {
      const projectData = await projectsApi.getById(projectId);
      if (projectData) {
        setProject(projectData);
      }
    } catch (err) {
      console.error("Failed to refresh project", err);
    }
  };

  if (loading) {
    return (
      <div className={styles.container}>
        <div className={styles.spinner}>
          <Spinner />
          <p>{t("loading")}</p>
        </div>
      </div>
    );
  }

  if (error || !project) {
    return (
      <div className={styles.container}>
        <div className={styles.error}>
          <p>{error || t("notFound")}</p>
          <button onClick={() => navigate("/projects")}>{t("back")}</button>
        </div>
      </div>
    );
  }

  const header = (
    <Stack gap="6">
      <Row align="center" gap="4">
        <ProjectAvatar
          name={project.name}
          description={project.description}
          showText={false}
        />
        <Stack gap="2">
          <h1 className={styles.title}>{project.name}</h1>
          {project.description && (
            <p className={styles.description}>{project.description}</p>
          )}
        </Stack>
      </Row>
      <div className={styles.statsAndButton}>
        <div className={styles.stats}>
          <Row>

          <div className={styles.statItem}>
            <span className={styles.statLabel}>{t("environments")}</span>
            <span className={styles.statValue}>{project.environmentCount}</span>
          </div>
          <div className={styles.statItem}>
            <span className={styles.statLabel}>{t("services")}</span>
            <span className={styles.statValue}>{project.serviceCount}</span>
          </div>
          </Row>
        </div>
        <Button
          variant="primary"
          onClick={() => setIsConfigOpen(true)}
        >
          Configure
        </Button>
      </div>
    </Stack>
  );

  const configHeader = (
    <div className={styles.configHeaderContent}>
      <h2>{t("settings")}</h2>
    </div>
  );

  const environmentsContent = (
    <Stack gap="6">
      {environments.length === 0 ? (
        <div className={styles.emptyState}>
          <p className={styles.emptyMessage}>{t("noEnvironments")}</p>
          <Button
            variant="primary"
            icon={<Plus size={20} />}
            onClick={() => setIsCreateEnvModalOpen(true)}
          >
            Add Environment
          </Button>
        </div>
      ) : (
        <>
          <div className={styles.environmentsHeader}>
            <Button
              variant="primary"
              icon={<Plus size={20} />}
              onClick={() => setIsCreateEnvModalOpen(true)}
            >
              Add Environment
            </Button>
          </div>
          <Grid gap="4">
            {environments.map((env) => (
              <EnvironmentCard
                key={env.id}
                environment={env}
                serviceCount={env.serviceCount}
                onClick={(projId, envId) =>
                  navigate(`/projects/${projId}/environments/${envId}`)
                }
                onEdit={(environment) => {
                  setEditingEnvironment(environment);
                  setIsCreateEnvModalOpen(true);
                }}
              />
            ))}
          </Grid>
        </>
      )}
    </Stack>
  );

  const menuItems = [
    {
      id: "variables",
      label: t("variables"),
      content: projectId ? (
        <EnvironmentVariablesEditor projectId={projectId} />
      ) : null,
    },
    {
      id: "settings",
      label: t("settings"),
      content: project ? (
        <ProjectSettingsForm
          project={project}
          onSuccess={handleProjectUpdated}
        />
      ) : null,
    },
  ];

  return (
    <>
      <ConfigurationPageLayout
        mainHeader={header}
        configHeader={configHeader}
        menuItems={menuItems}
        isConfigOpen={isConfigOpen}
        onConfigOpenChange={setIsConfigOpen}
        hideConfigButton={true}
      >
        {environmentsContent}
      </ConfigurationPageLayout>

      {projectId && (
        <CreateEnvironmentModal
          projectId={projectId}
          isOpen={isCreateEnvModalOpen}
          onClose={() => {
            setIsCreateEnvModalOpen(false);
            setEditingEnvironment(null);
          }}
          onSuccess={handleCreateEnvironmentSuccess}
          environment={editingEnvironment || undefined}
        />
      )}
    </>
  );
}
