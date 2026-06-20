import { useState, useRef, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, Loader } from 'lucide-react';
import { useFuzzySearch } from '@/hooks/useFuzzySearch';
import { FuzzySearchResult } from '@/api/types';
import { Button } from '../ui/Button';
import { Badge } from '../ui/Badge';
import styles from './FuzzySearchBar.module.css';

export function FuzzySearchBar() {
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [selectedIndex, setSelectedIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const navigate = useNavigate();
  const { results, isLoading } = useFuzzySearch(query);

  const isMac =
    typeof window !== 'undefined' && navigator.platform.toUpperCase().indexOf('MAC') >= 0;
  const hotkey = isMac ? '⌘K' : 'Ctrl + K';

  useEffect(() => {
    if (isOpen && inputRef.current) {
      inputRef.current.focus();
    }
  }, [isOpen]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((isMac && e.metaKey && e.key === 'k') || (!isMac && e.ctrlKey && e.key === 'k')) {
        e.preventDefault();
        setIsOpen(prev => !prev);
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isMac]);

  const handleOpen = () => {
    setIsOpen(true);
    setQuery('');
  };

  const handleClose = () => {
    setIsOpen(false);
    setQuery('');
    setSelectedIndex(0);
  };

  const handleNavigate = (result: FuzzySearchResult) => {
    const url = buildNavigationUrl(result);
    if (url) {
      navigate(url);
      handleClose();
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') {
      handleClose();
      return;
    }

    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setSelectedIndex(prev => (prev + 1) % results.length);
      return;
    }

    if (e.key === 'ArrowUp') {
      e.preventDefault();
      setSelectedIndex(prev => (prev - 1 + results.length) % results.length);
      return;
    }

    if (e.key === 'Enter' && results.length > 0) {
      e.preventDefault();
      handleNavigate(results[selectedIndex]);
    }
  };

  return (
    <>
      <Button variant="ghost" size="sm" className={styles.trigger} onClick={handleOpen}>
        <Search size={20} />
        <span className={styles.placeholder}>Search...</span>
        <span className={styles.hotkey}>{hotkey}</span>
      </Button>

      {isOpen && (
        <div className={styles.backdrop} onClick={() => handleClose()}>
          <div className={styles.modal} onClick={e => e.stopPropagation()}>
            <div className={styles.searchInput}>
              <input
                ref={inputRef}
                type="text"
                placeholder="Search projects, environments, services..."
                value={query}
                onChange={e => {
                  setQuery(e.target.value);
                  setSelectedIndex(0);
                }}
                onKeyDown={handleKeyDown}
                className={styles.input}
              />
              {isLoading && <Loader size={18} className={styles.loader} />}
            </div>

            {query && (
              <div className={styles.results}>
                {results.length > 0 ? (
                  results.map((result, index) => {
                    const route = getResultRoute(result);
                    return (
                      <button
                        key={`${result.entityType}-${result.id}`}
                        className={`${styles.resultItem} ${
                          index === selectedIndex ? styles.selected : ''
                        }`}
                        onClick={() => handleNavigate(result)}
                      >
                        <div className={styles.resultContent}>
                          <div className={styles.labelAndRoute}>
                            <span className={styles.label}>{result.label}</span>
                            <span className={styles.route}>{route}</span>
                          </div>
                        </div>
                        <Badge
                          variant={getBadgeVariant(result.entityType)}
                          className={styles.entityBadge}
                        >
                          {result.entityType}
                        </Badge>
                      </button>
                    );
                  })
                ) : !isLoading ? (
                  <div className={styles.emptyState}>No results found for "{query}"</div>
                ) : null}
              </div>
            )}
          </div>
        </div>
      )}
    </>
  );
}

function buildNavigationUrl(result: FuzzySearchResult): string | null {
  switch (result.entityType) {
    case 'Project':
      return `/projects/${result.id}`;

    case 'Environment': {
      const projectId = result.metadata?.projectId;
      if (projectId) {
        return `/projects/${projectId}/environments/${result.id}`;
      }
      return null;
    }

    case 'Service': {
      const projectId = result.metadata?.projectId;
      const environmentId = result.metadata?.environmentId;
      if (projectId && environmentId) {
        return `/projects/${projectId}/environments/${environmentId}/services/${result.id}`;
      }
      return null;
    }

    default:
      return null;
  }
}

function getResultRoute(result: FuzzySearchResult): string {
  switch (result.entityType) {
    case 'Project':
      return '/projects/:projectId';

    case 'Environment': {
      const projectId = result.metadata?.projectId;
      if (projectId) {
        return `/projects/${projectId}/environments/:environmentId`;
      }
      return '';
    }

    case 'Service': {
      const projectId = result.metadata?.projectId;
      const environmentId = result.metadata?.environmentId;
      if (projectId && environmentId) {
        return `/projects/${projectId}/environments/${environmentId}/services/:serviceId`;
      }
      return '';
    }

    default:
      return '';
  }
}

function getBadgeVariant(
  entityType: string
): 'default' | 'primary' | 'success' | 'warning' | 'danger' {
  switch (entityType) {
    case 'Project':
      return 'primary';
    case 'Environment':
      return 'success';
    case 'Service':
      return 'warning';
    default:
      return 'default';
  }
}
