import { ServiceTemplateSummaryDto } from '@/api/types';

import { Modal } from '../ui/Modal';
import { ServiceTemplatePicker } from './ServiceTemplatePicker';

interface TemplatePickerModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSelect: (template: ServiceTemplateSummaryDto) => void;
  selectedTemplateId?: string | null;
}

export function TemplatePickerModal({
  isOpen,
  onClose,
  onSelect,
  selectedTemplateId,
}: TemplatePickerModalProps) {
  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Pick a Template"
      description="Choose a built-in template to configure your service from."
      size="lg"
    >
      <ServiceTemplatePicker
        selectedTemplateId={selectedTemplateId}
        onSelect={template => {
          onSelect(template);
          onClose();
        }}
        autoFocusSearch
      />
    </Modal>
  );
}
