import { Database, Layers } from 'lucide-react';
import { useMemo, useState } from 'react';

import { ServiceTemplateSummaryDto } from '@/api/types';
import { useServiceTemplates } from '@/hooks/useServiceTemplates';
import styles from '@/styles/components/services/ServiceTemplatePicker.module.css';

import { FilterPillGroup } from '../ui/FilterPillGroup';
import { SearchInput } from '../ui/SearchInput';
import { Spinner } from '../ui/Spinner';

function fallbackIconFor(template: ServiceTemplateSummaryDto) {
  switch (template.category.toLowerCase()) {
    case 'database':
      return <Database size={28} />;
    default:
      return <Layers size={28} />;
  }
}

// Icon assets live in /public/icons and are named after the template's `icon` field
// (e.g. "redis.svg"). Drop a matching svg in there to give a new template its own icon.
function TemplateIcon({ template }: { template: ServiceTemplateSummaryDto }) {
  const [failed, setFailed] = useState(false);

  if (!template.icon || failed) {
    return fallbackIconFor(template);
  }

  return (
    <img
      src={`/icons/${template.icon}`}
      alt=""
      width={28}
      height={28}
      onError={() => setFailed(true)}
    />
  );
}

const ALL_CATEGORIES = '__all__';

function matches(template: ServiceTemplateSummaryDto, query: string, category: string) {
  if (category !== ALL_CATEGORIES && template.category.toLowerCase() !== category.toLowerCase()) {
    return false;
  }

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
  const [category, setCategory] = useState(ALL_CATEGORIES);

  const categoryOptions = useMemo(() => {
    const unique = new Set((templates ?? []).map(t => t.category));
    const sorted = Array.from(unique).sort((a, b) => a.localeCompare(b));
    return [{ value: ALL_CATEGORIES, label: 'All' }, ...sorted.map(c => ({ value: c, label: c }))];
  }, [templates]);

  const filtered = useMemo(
    () => (templates ?? []).filter(t => matches(t, search, category)),
    [templates, search, category]
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

      {categoryOptions.length > 1 && (
        <FilterPillGroup options={categoryOptions} value={category} onChange={setCategory} />
      )}

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
              <div className={styles.templateIcon}>
                <TemplateIcon template={template} />
              </div>
              <span className={styles.templateName}>{template.name}</span>
              <span className={styles.templateCategory}>{template.category}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
