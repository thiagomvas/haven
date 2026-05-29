import styles from './AboutPage.module.css'

export function AboutPage() {
  return (
    <div className={styles.container}>
      <h2 className={styles.title}>Haven</h2>
      <p className={styles.version}>Version 1.0.0</p>
      <p className={styles.description}>
        Haven is a tool for managing containerized services across projects and environments.
      </p>
    </div>
  )
}
