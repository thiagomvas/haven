import styles from '@/styles/components/ui/ToggleChip.module.css';

interface ToggleChipProps {
  checked: boolean;
  onLabel: string;
  offLabel: string;
  onChange?: (checked: boolean) => void;
  disabled?: boolean;
}

export function ToggleChip({ checked, onLabel, offLabel, onChange, disabled }: ToggleChipProps) {
  const isInteractive = !!onChange;

  const handleClick = () => {
    if (!disabled && onChange) onChange(!checked);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if ((e.key === 'Enter' || e.key === ' ') && !disabled && onChange) {
      e.preventDefault();
      onChange(!checked);
    }
  };

  return (
    <span
      className={`${styles.chip} ${checked ? styles.enabled : styles.disabled} ${isInteractive ? styles.interactive : ''}`}
      role={isInteractive ? 'switch' : undefined}
      aria-checked={isInteractive ? checked : undefined}
      aria-disabled={disabled}
      onClick={isInteractive ? handleClick : undefined}
      onKeyDown={isInteractive ? handleKeyDown : undefined}
      tabIndex={isInteractive && !disabled ? 0 : undefined}
    >
      <span className={styles.dot} />
      {checked ? onLabel : offLabel}
    </span>
  );
}
