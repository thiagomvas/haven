import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { Globe, Plus, Rocket, Settings, SquareAsterisk } from "lucide-react";
import { useSetBreadcrumbs } from "@/hooks/useSetBreadcrumbs";
import { usePermission } from "@/hooks/usePermission";
import { PermissionGuard } from "@/components/PermissionGuard";
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
import { DegradedServicesChip } from "@/components/ui/chips/degradedServicesChip";
import { ServiceChip } from "@/components/ui/chips/ServiceChip";
import { Chip } from "@/components/ui/Chip";
import { Divider } from "@/components/ui/Divider";

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
  const [selectedMenuId, setSelectedMenuId] = useState<string>("settings");
  const canUpdateProject = usePermission("projects.update");
  const canCreateEnvironment = usePermission("environments.create");
  const canDeployService = usePermission("services.deploy");

  useSetBreadcrumbs([
    { label: "Projects", to: "/projects" },
    { label: project?.name ?? "…" },
  ])

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
    stats: typeof project.serviceStatistics,
  ): string => {
    const { total, running } = stats;
    if (running === total && total > 0) {
      return tCommon("statuses.allServicesRunning");
    }
    if (running === 0) {
      return tCommon("statuses.noServicesRunning");
    }
    return tCommon("statuses.someServices", {
      running,
      total,
    });
  };

  const getEnvironmentServiceStatus = (
    stats: typeof project.serviceStatistics,
  ): { color: string; status: string } => {
    const { total, running } = stats;
    if (total === 0) {
      return { color: "var(--color-text-muted)", status: "unknown" };
    }
    if (running === total) {
      return { color: "var(--color-running)", status: "running" };
    }
    if (running === 0) {
      return { color: "var(--color-stopped)", status: "stopped" };
    }
    return { color: "var(--color-degraded)", status: "degraded" };
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
          <Row>
            <h1 className={styles.title}>{project.name}</h1>
            <DegradedServicesChip count={project.serviceStatistics.degraded} />
            <Spacer expand direction="horizontal" />
            {canUpdateProject && (
              <Button
                variant="text"
                size="sm"
                icon={<Settings size={16} />}
                onClick={() => setIsConfigOpen(!isConfigOpen)}
              >
                {isConfigOpen ? t("closeSettings") : t("settings")}
              </Button>
            )}
            {canDeployService && (
              <Button
                variant="primary"
                size="sm"
                icon={<Rocket size={16} />}
                disabled
              >
                {t("deployAll")}
              </Button>
            )}
          </Row>
          {project.description && (
            <p className={styles.description}>{project.description}</p>
          )}
        </Stack>
      </Row>
    </Card>
  );

  const environmentsContent = project ? (
    <Grid columns={2} columnTemplate="1.5fr 1fr">
      <Stack>
        <Card padding="var(--space-4)">
          <Row align="center" gap="2">
            <Row gap="2" align="center" full>
              <Globe size={16} />
              {tCommon("labels.environments")}
              <Chip
                variant="default"
                size="sm"
                content={project.environments.length}
              />
              <Spacer expand direction="horizontal" />
              {canCreateEnvironment && (
                <Button
                  variant="secondary"
                  onClick={() => navigate(`/environments/create?projectId=${projectId}`)}
                >
                  <Plus size={16} />
                  Add
                </Button>
              )}
            </Row>
          </Row>
          {project.environments.length > 0 ? (
            project.environments.map((env) => {
              const serviceStatus = getEnvironmentServiceStatus(
                env.serviceStatistics,
              );
              return (
                <Card
                  padding="var(--space-3)"
                  style={{ marginTop: "var(--space-3)" }}
                  className={styles.environmentCard}
                  key={env.id}
                  onClick={() => navigate(`/projects/${projectId}/environments/${env.id}`)}
                >
                  <CardTitle>
                    <Row full align="center" gap="2">
                      <HealthIndicator health={env.status.toLowerCase()} />
                      {env.name}
                      <Chip
                        variant="default"
                        size="sm"
                        content={env.networkName}
                      />
                    </Row>
                    <p>
                      <span
                        style={{
                          color: serviceStatus.color,
                          fontWeight: 600,
                          fontSize: "var(--font-size-sm)",
                        }}
                      >
                        {env.serviceStatistics.running}
                      </span>

                      <span
                        style={{
                          color: "var(--color-text-muted)",
                          fontWeight: 400,
                          fontSize: "var(--font-size-xs)",
                        }}
                      >
                        / {env.serviceStatistics.total} services
                      </span>
                    </p>
                    <Divider />
                    <div
                      style={{
                        marginTop: "var(--space-3)",
                        display: "flex",
                        gap: "var(--space-2)",
                        flexWrap: "wrap",
                      }}
                    >
                      {" "}
                      {env.services.map((service) => (
                        <ServiceChip
                          key={service.id}
                          size="sm"
                          serviceName={service.name}
                          health={service.status.toLowerCase()}
                        />
                      ))}
                    </div>
                  </CardTitle>
                </Card>
              );
            })
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
              <strong>{project.serviceStatistics.total}</strong>
            </div>
            <div style={{ display: "flex", justifyContent: "space-between" }}>
              <span style={{ color: "var(--color-text-secondary)" }}>
                Running
              </span>
              <strong>{project.serviceStatistics.running}</strong>
            </div>
            <div style={{ display: "flex", justifyContent: "space-between" }}>
              <span style={{ color: "var(--color-text-secondary)" }}>
                Stopped
              </span>
              <strong>{project.serviceStatistics.stopped}</strong>
            </div>
            <div style={{ display: "flex", justifyContent: "space-between" }}>
              <span style={{ color: "var(--color-text-secondary)" }}>
                Degraded
              </span>
              <strong>{project.serviceStatistics.degraded}</strong>
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
          </Stack>
        </Card>
        <Card padding="var(--space-3)">
          <CardTitle>
            <Row gap="2" align="center">
              <SquareAsterisk size={16} />
              {tCommon("labels.variables")} <Chip variant="default" size="sm" content={project.environmentVariables.length} />
            </Row>
          </CardTitle>
          {project.environmentVariables.length > 0 ? (
            <div style={{ marginTop: "var(--space-3)" }}>
              <table
                style={{
                  width: "100%",
                  borderCollapse: "collapse",
                  tableLayout: "auto",
                }}
              >
                <tbody>
                  {project.environmentVariables.slice(0, 5).map((variable) => (
                    <tr
                      key={variable.key}
                      style={{ borderBottom: "1px solid var(--color-border)" }}
                    >
                      <td
                        style={{
                          padding: "var(--space-2)",
                          maxWidth: "120px",
                          overflow: "hidden",
                          textOverflow: "ellipsis",
                          whiteSpace: "nowrap",
                        }}
                        title={variable.key}
                      >
                        {variable.key}
                      </td>
                      <td
                        style={{
                          padding: "var(--space-2)",
                          maxWidth: "200px",
                          overflow: "hidden",
                          textOverflow: "ellipsis",
                          whiteSpace: "nowrap",
                          textAlign: "right",
                          color: "var(--color-text-secondary)",
                        }}
                        title={variable.value}
                      >
                        {variable.value}
                      </td>
                      <td
                        style={{
                          padding: "var(--space-2)",
                          width: "fit-content",
                          textAlign: "right",
                          color: "var(--color-text-muted)",
                          fontSize: "var(--font-size-xs)",
                          whiteSpace: "nowrap",
                        }}
                      >
                        {variable.scope.toUpperCase()}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {project.environmentVariables.length > 5 && (
                <Row>
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => {
                      setSelectedMenuId("variables");
                      setIsConfigOpen(true);
                    }}
                  >
                    {tCommon("labels.viewAll")} (
                    {project.environmentVariables.length})
                  </Button>
                  <p
                    style={{
                      marginTop: "var(--space-2)",
                      color: "var(--color-text-muted)",
                      fontSize: "var(--font-size-xs)",
                    }}
                  >
                    {t("environmentVariableNotice")}
                  </p>
                </Row>
              )}
            </div>
          ) : (
            <p
              style={{
                padding: "var(--space-3)",
                color: "var(--color-text-secondary)",
                marginTop: "var(--space-3)",
              }}
            >
              No variables yet.
            </p>
          )}
        </Card>
      </Stack>
    </Grid>
  ) : null;

  const menuItems = [
    ...(canUpdateProject ? [{
      id: "settings",
      label: t("settings"),
      content: project ? (
        <ProjectSettingsForm
          project={project}
          onSuccess={handleProjectUpdated}
        />
      ) : null,
    }] : []),
    ...(canUpdateProject ? [{
      id: "variables",
      label: t("variables"),
      content: projectId ? (
        <EnvironmentVariablesEditor projectId={projectId} />
      ) : null,
    }] : []),
  ];

  return (
    <>
      <ConfigurationPageLayout
        mainHeader={header}
        configHeader={header}
        menuItems={menuItems}
        isConfigOpen={isConfigOpen}
        onConfigOpenChange={setIsConfigOpen}
        selectedMenuId={selectedMenuId}
        onSelectedMenuIdChange={setSelectedMenuId}
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
