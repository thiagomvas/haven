import { ExposureMode } from './service.types';

export type TemplateInputFieldType = 'Text' | 'Select' | 'Secret';

export interface TemplateInputFieldDto {
  key: string;
  type: TemplateInputFieldType;
  label?: string;
  defaultValue?: string;
  immutable: boolean;
  options?: string[];
}

export interface ServiceTemplateSummaryDto {
  id: string;
  version: string;
  name: string;
  icon: string;
  category: string;
}

export interface ServiceTemplateDto extends ServiceTemplateSummaryDto {
  inputs: TemplateInputFieldDto[];
}

export interface CreateServiceFromTemplateInput {
  name?: string;
  alias?: string;
  inputValues: Record<string, string>;
  exposureMode: ExposureMode;
  ports: string[];
}
