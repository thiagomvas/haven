import { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { NavLink } from "react-router-dom";
import {
  LayoutDashboard,
  FolderOpen,
  GitBranch,
  Clock,
  BarChart3,
  AlertCircle,
  Settings,
  HelpCircle,
  BookOpen,
  ChevronLeft,
  ChevronRight,
  PanelRightOpen,
  PanelRightClose,
} from "lucide-react";
import { Button } from "../ui/Button";
import { Tooltip } from "../ui/Tooltip";
import { usePermission } from "@/hooks/usePermission";
import styles from "./Sidebar.module.css";

interface SidebarProps {
  collapsed?: boolean;
  onToggleCollapse?: () => void;
}

interface NavItem {
  to: string;
  icon: ReactNode;
  label: string;
  translationKey: string;
}

export function Sidebar({ collapsed = false, onToggleCollapse }: SidebarProps) {
  const { t } = useTranslation("layout");
  const canViewProjects = usePermission("projects.read");
  const canViewCredentials = usePermission("system.read_git_credentials");

  const mainNavItems: NavItem[] = [
    {
      to: "/dashboard",
      icon: <LayoutDashboard size={20} />,
      label: "Dashboard",
      translationKey: "sidebar.dashboard",
    },
    ...(canViewProjects ? [{
      to: "/projects",
      icon: <FolderOpen size={20} />,
      label: "Projects",
      translationKey: "sidebar.projects",
    }] : []),
  ];

  const systemNavItems: NavItem[] = [
    ...(canViewCredentials ? [{
      to: "/git-providers",
      icon: <GitBranch size={20} />,
      label: "Git Providers",
      translationKey: "sidebar.gitProviders",
    }] : []),
  ];

  const NavLinkContent = ({
    icon,
    label,
  }: {
    icon: ReactNode;
    label: string;
  }) => (
    <>
      {collapsed ? (
        <Tooltip content={label}>{icon}</Tooltip>
      ) : (
        <>
          {icon}
          <span>{label}</span>
        </>
      )}
    </>
  );

  const renderNavItem = (item: NavItem) => (
    <NavLink
      key={item.to}
      to={item.to}
      className={({ isActive }) =>
        `${styles.navLink} ${isActive ? styles.navLinkActive : ""}`
      }
    >
      <NavLinkContent icon={item.icon} label={t(item.translationKey as any)} />
    </NavLink>
  );

  const renderSection = (items: NavItem[]) => items.map(renderNavItem);

  return (
    <aside
      className={`${styles.sidebar} ${collapsed ? styles.sidebarCollapsed : ""}`}
    >
      <div className={styles.sidebarHeader}>
        <Button
          variant="ghost"
          size="sm"
          onClick={onToggleCollapse}
          title={t("header.toggleSidebar")}
          className={styles.toggleButton}
        >
          {collapsed ? (
            <PanelRightOpen size={20} />
          ) : (
            <PanelRightClose size={20} />
          )}
        </Button>
      </div>
      <nav className={styles.navContainer}>
        <div className={styles.navSection}>
          <div className={styles.sectionTitle}>
            {collapsed ? "" : t("sidebar.main")}
          </div>
          <div className={styles.navItems}>{renderSection(mainNavItems)}</div>
        </div>

        {systemNavItems.length > 0 && (
          <div className={styles.navSection}>
            <div className={styles.sectionTitle}>
              {collapsed ? "" : t("sidebar.system")}
            </div>
            <div className={styles.navItems}>{renderSection(systemNavItems)}</div>
          </div>
        )}
      </nav>

      <div className={styles.sidebarFooter}>
        <NavLink
          to="/settings"
          className={({ isActive }) =>
            `${styles.configButton} ${isActive ? styles.configButtonActive : ""}`
          }
          title={t("sidebar.settings")}
          aria-label={t("sidebar.settings")}
        >
          <Tooltip content={t("sidebar.settings")}>
            <Settings size={20} />
          </Tooltip>
        </NavLink>
      </div>
    </aside>
  );
}
