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
import {
  Row,
  ConfigurationPageLayout,
  Stack,
  Grid,
  Spacer,
  Table,
  TableHeader,
  TableRow,
  TableCell,
} from "@/components/layout";
import { Card, CardTitle } from "@/components/ui/Card";

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
    <Card style={{ width: "100%", padding: "var(--space-4)" }}>
      <Row align="center" gap="4" full>
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
        <Spacer expand direction="horizontal" />
        <Button
          variant="primary"
          onClick={() => setIsConfigOpen(!isConfigOpen)}
        >
          Configure
        </Button>
      </Row>
    </Card>
  );

  const environmentsContent = (
    <Grid columns={2} columnTemplate="1.5fr 1fr">
      <Stack>
        <Card padding="var(--space-4)">
          <CardTitle>Environments</CardTitle>
          {environments.length > 0 ? (
            <Table striped hoverable padding="2">
              <thead>
                <TableRow isHeader>
                  <TableHeader>Environment</TableHeader>
                  <TableHeader>Services</TableHeader>
                  <TableHeader>Shared Networks</TableHeader>
                </TableRow>
              </thead>
              <tbody>
                {environments.map((env) => (
                  <TableRow key={env.id} onRowClick={() => {
                    navigate(`/projects/${projectId}/environments/${env.id}`)
                  }}>
                    <TableCell variant="default">
                      <Stack gap="1">
                        <span>{env.name}</span>
                        {env.description && (
                          <span style={{ fontSize: "var(--font-size-sm)", color: "var(--color-text-secondary)" }}>
                            {env.description}
                          </span>
                        )}
                      </Stack>
                    </TableCell>
                    <TableCell variant="muted">{`${env.serviceCount || 0}`}</TableCell>
                    <TableCell variant="muted">{'-'}</TableCell>
                  </TableRow>
                ))}
              </tbody>
            </Table>
          ) : (
            <p style={{ padding: "var(--space-3)", color: "var(--color-text-secondary)" }}>
              No environments yet
            </p>
          )}
        </Card>
      </Stack>
      <Stack gap="2">
        <Card padding="var(--space-2)">
          <CardTitle>Project Settings</CardTitle>
        </Card>
        <Card padding="var(--space-2)">
          <CardTitle>Environment 3</CardTitle>
        </Card>
        <Card padding="var(--space-2)">
          <CardTitle>Environment 3</CardTitle>
        </Card>
      </Stack>
    </Grid>
  );

  const menuItems = [
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
    {
      id: "variables",
      label: t("variables"),
      content: projectId ? (
        <EnvironmentVariablesEditor projectId={projectId} />
      ) : null,
    },
  ];

  return (
    <>
      <ConfigurationPageLayout
        mainHeader={header}
        configHeader={header}
        menuItems={menuItems}
        isConfigOpen={isConfigOpen}
        onConfigOpenChange={setIsConfigOpen}
        hideConfigButton={true}
        hideCloseButton={true}
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
