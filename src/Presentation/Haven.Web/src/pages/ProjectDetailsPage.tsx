import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { Plus, Settings } from "lucide-react";
import { projectsApi } from "../api/projects";
import { ProjectDashboardDto, EnvironmentDashboardDto } from "../api/types";
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
import { HealthIndicator } from "@/components/ui/HealthIndicator";
import { Tooltip } from "@/components/ui/Tooltip";

export function ProjectDetailsPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation("projects");
  const { t: tCommon } = useTranslation("common");

  const [project, setProject] = useState<ProjectDashboardDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isCreateEnvModalOpen, setIsCreateEnvModalOpen] = useState(false);
  const [isConfigOpen, setIsConfigOpen] = useState(false);

  useEffect(() => {
    const loadProjectData = async () => {
      if (!projectId) return;

      try {
        setLoading(true);
        setError(null);

        const projectData = await projectsApi.getDashboardById(projectId);

        if (!projectData) {
          setError("Project not found");
          return;
        }

        setProject(projectData);
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
      const projectData = await projectsApi.getDashboardById(projectId);
      if (projectData) {
        setProject(projectData);
      }
    } catch (err) {
      console.error("Failed to refresh project dashboard", err);
    }
  };

  const handleProjectUpdated = async () => {
    if (!projectId) return;
    try {
      const projectData = await projectsApi.getDashboardById(projectId);
      if (projectData) {
        setProject(projectData);
      }
    } catch (err) {
      console.error("Failed to refresh project dashboard", err);
    }
  };

  const getEnvironmentStatusMessage = (
    servicesRunning: number,
    totalServices: number,
  ): string => {
    if (servicesRunning === totalServices && totalServices > 0) {
      return tCommon("statuses.allServicesRunning");
    }
    if (servicesRunning === 0) {
      return tCommon("statuses.noServicesRunning");
    }
    return tCommon("statuses.someServices", {
      running: servicesRunning,
      total: totalServices,
    });
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

  const environmentsContent = project ? (
    <Grid columns={2} columnTemplate="1.5fr 1fr">
      <Stack>
        <Card padding="var(--space-4)">
          <Row align="center" gap="2">
            <CardTitle>Environments</CardTitle>
            <Spacer expand direction="horizontal" />
            <Button
              variant="secondary"
              onClick={() => setIsCreateEnvModalOpen(true)}
            >
              <Plus size={16} />
              Add
            </Button>
          </Row>
          {project.environments.length > 0 ? (
            <Table striped hoverable padding="2">
              <thead>
                <TableRow isHeader>
                  <TableHeader>Environment</TableHeader>
                  <TableHeader>Networks</TableHeader>
                  <TableHeader>Services</TableHeader>
                </TableRow>
              </thead>
              <tbody>
                {project.environments.map((env) => (
                  <TableRow
                    key={env.id}
                    onRowClick={() => {
                      navigate(`/projects/${projectId}/environments/${env.id}`);
                    }}
                  >
                    <TableCell variant="default">
                      <Tooltip
                        content={getEnvironmentStatusMessage(
                          env.servicesRunning,
                          env.totalServices,
                        )}
                      >
                        <HealthIndicator health={env.status.toLowerCase()} />
                      </Tooltip>

                      <span style={{ paddingLeft: "var(--space-2)" }}>
                        {env.name}
                      </span>
                    </TableCell>
                    <TableCell variant="muted">
                      {env.networkName || "N/A"}
                    </TableCell>
                    <TableCell variant="muted">
                      {`${env.servicesRunning}/${env.totalServices}`}
                    </TableCell>
                  </TableRow>
                ))}
              </tbody>
            </Table>
          ) : (
            <p
              style={{
                padding: "var(--space-3)",
                color: "var(--color-text-secondary)",
              }}
            >
              No environments yet. Create one to get started.
            </p>
          )}
        </Card>
      </Stack>
      <Stack gap="2">
        <Card padding="var(--space-3)">
          <CardTitle>
            <Row gap="2" align="center">
              <Settings size={16} />
              Project Info
            </Row>
          </CardTitle>
          <Stack gap="2" style={{ marginTop: "var(--space-3)" }}>
            <div style={{ display: "flex", justifyContent: "space-between" }}>
              <span style={{ color: "var(--color-text-secondary)" }}>
                Total Services
              </span>
              <strong>{project.totalServices}</strong>
            </div>
            <div style={{ display: "flex", justifyContent: "space-between" }}>
              <span style={{ color: "var(--color-text-secondary)" }}>
                Running
              </span>
              <strong>{project.totalServicesRunning}</strong>
            </div>
            {project.lastDeployedAt && (
              <div style={{ display: "flex", justifyContent: "space-between" }}>
                <span style={{ color: "var(--color-text-secondary)" }}>
                  Last Deployed
                </span>
                <span>
                  {new Date(project.lastDeployedAt).toLocaleDateString()}
                </span>
              </div>
            )}
            <div style={{ display: "flex", justifyContent: "space-between" }}>
              <span style={{ color: "var(--color-text-secondary)" }}>
                {tCommon("labels.variables")} ({tCommon("labels.project")})
              </span>
              <strong>{project.totalEnvVars}</strong>
            </div>
          </Stack>
        </Card>
      </Stack>
    </Grid>
  ) : null;

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
          onClose={() => setIsCreateEnvModalOpen(false)}
          onSuccess={handleCreateEnvironmentSuccess}
        />
      )}
    </>
  );
}
