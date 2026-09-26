import { Database, Layers } from 'lucide-react';
import { useMemo, useState } from 'react';

import { ServiceTemplateSummaryDto } from '@/api/types';
import { useServiceTemplates } from '@/hooks/useServiceTemplates';
import styles from '@/styles/components/services/ServiceTemplatePicker.module.css';

import { SearchInput } from '../ui/SearchInput';
import { Spinner } from '../ui/Spinner';

function iconFor(template: ServiceTemplateSummaryDto) {
  switch (template.category.toLowerCase()) {
    case 'database':
      return <Database size={28} />;
    default:
      return <Layers size={28} />;
  }
}

function matches(template: ServiceTemplateSummaryDto, query: string) {
  const q = query.trim().toLowerCase();
  if (!q) return true;
  return template.name.toLowerCase().includes(q) || template.category.toLowerCase().includes(q);
}

interface ServiceTemplatePickerProps {
  selectedTemplateId?: string | null;
  onSelect: (template: ServiceTemplateSummaryDto) => void;
  disabled?: boolean;
  autoFocusSearch?: boolean;
}

export function ServiceTemplatePicker({
  selectedTemplateId,
  onSelect,
  disabled,
  autoFocusSearch,
}: ServiceTemplatePickerProps) {
  const { data: templates, isLoading, error } = useServiceTemplates();
  const [search, setSearch] = useState('');

  const filtered = useMemo(
    () => (templates ?? []).filter(t => matches(t, search)),
    [templates, search]
  );

  if (isLoading) {
    return <Spinner />;
  }

  if (error) {
    return <p className={styles.errorText}>Failed to load service templates.</p>;
  }

  if (!templates || templates.length === 0) {
    return <p className={styles.errorText}>No service templates are available.</p>;
  }

  return (
    <div className={styles.pickerContainer}>
      <SearchInput
        placeholder="Search templates by name or category…"
        value={search}
        onChange={e => setSearch(e.target.value)}
        autoFocus={autoFocusSearch}
        wrapperClassName={styles.searchWrapper}
      />

      {filtered.length === 0 ? (
        <p className={styles.errorText}>No templates match "{search}".</p>
      ) : (
        <div className={styles.templateGrid}>
          {filtered.map(template => (
            <button
              key={template.id}
              type="button"
              className={`${styles.templateCard} ${template.id === selectedTemplateId ? styles.selected : ''}`}
              onClick={() => onSelect(template)}
              disabled={disabled}
            >
              <div className={styles.templateIcon}>{iconFor(template)}</div>
              <span className={styles.templateName}>{template.name}</span>
              <span className={styles.templateCategory}>{template.category}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
