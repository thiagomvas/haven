import { Plus, X } from 'lucide-react';
import { useState } from 'react';

import { DockerConfig } from '@/api/types/service.types';

import { Button } from '../ui/Button';
import styles from './DockerConfigForm.module.css';

interface DockerConfigFormProps {
  config: DockerConfig | undefined;
  onSave: (config: DockerConfig) => Promise<void>;
  isLoading?: boolean;
}

export function DockerConfigForm({ config, onSave, isLoading = false }: DockerConfigFormProps) {
  const [formData, setFormData] = useState<DockerConfig>(
    config || {
      image: '',
      ports: [],
      restartPolicy: 'UnlessStopped',
    }
  );
  const [errors, setErrors] = useState<Record<string, string>>({});

  const handleImageChange = (value: string) => {
    setFormData(prev => ({ ...prev, image: value }));
    if (value.trim()) setErrors(prev => ({ ...prev, image: '' }));
  };

  const handleAddPort = () => {
    setFormData(prev => ({
      ...prev,
      ports: [...prev.ports, ''],
    }));
  };

  const handleRemovePort = (index: number) => {
    setFormData(prev => ({
      ...prev,
      ports: prev.ports.filter((_, i) => i !== index),
    }));
  };

  const handlePortChange = (index: number, value: string) => {
    setFormData(prev => {
      const newPorts = [...prev.ports];
      newPorts[index] = value;
      return { ...prev, ports: newPorts };
    });
  };

  const handleRestartPolicyChange = (value: string) => {
    setFormData(prev => ({
      ...prev,
      restartPolicy: value as 'No' | 'Always' | 'UnlessStopped' | 'OnFailure',
    }));
  };

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.image.trim()) {
      newErrors.image = 'Image is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (!validate()) return;

    try {
      await onSave(formData);
    } catch (err) {
      console.error('Failed to save configuration', err);
    }
  };

  return (
    <div className={styles.form}>
      <div className={styles.section}>
        <label className={styles.label}>
          <span className={styles.labelText}>Docker Image *</span>
          <input
            type="text"
            className={`${styles.input} ${errors.image ? styles.inputError : ''}`}
            value={formData.image}
            onChange={e => handleImageChange(e.target.value)}
            placeholder="e.g., nginx:latest"
            disabled={isLoading}
          />
          {errors.image && <span className={styles.error}>{errors.image}</span>}
        </label>
      </div>

      <div className={styles.section}>
        <div className={styles.sectionHeader}>
          <h3 className={styles.sectionTitle}>Ports</h3>
          <Button
            variant="secondary"
            size="sm"
            icon={<Plus size={16} />}
            onClick={handleAddPort}
            disabled={isLoading}
          >
            Add Port
          </Button>
        </div>
        {formData.ports.length === 0 ? (
          <p className={styles.emptyText}>No ports configured</p>
        ) : (
          <div className={styles.itemList}>
            {formData.ports.map((port, index) => (
              <div key={index} className={styles.item}>
                <input
                  type="text"
                  className={styles.input}
                  value={port}
                  onChange={e => handlePortChange(index, e.target.value)}
                  placeholder="e.g., 8080:80"
                  disabled={isLoading}
                />
                <button
                  className={styles.removeButton}
                  onClick={() => handleRemovePort(index)}
                  disabled={isLoading}
                  title="Remove"
                >
                  <X size={16} />
                </button>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className={styles.section}>
        <label className={styles.label}>
          <span className={styles.labelText}>Restart Policy</span>
          <select
            className={styles.select}
            value={formData.restartPolicy}
            onChange={e => handleRestartPolicyChange(e.target.value)}
            disabled={isLoading}
          >
            <option value="No">No</option>
            <option value="Always">Always</option>
            <option value="UnlessStopped">Unless Stopped</option>
            <option value="OnFailure">On Failure</option>
          </select>
        </label>
      </div>

      <div className={styles.actions}>
        <Button variant="primary" onClick={handleSubmit} isLoading={isLoading} disabled={isLoading}>
          Save Configuration
        </Button>
      </div>
    </div>
  );
}
