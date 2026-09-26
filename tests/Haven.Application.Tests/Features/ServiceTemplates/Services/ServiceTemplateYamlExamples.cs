namespace Haven.Application.Tests.Features.ServiceTemplates.Services;

public static class ServiceTemplateYamlExamples
{
    public const string CompletePostgres = @"
id: postgres
version: 1.0.0
name: PostgreSQL
icon: postgres.svg
category: database
inputs:
    - key: postgres_version
      type: select
      options: [""9.6"", ""10"", ""11"", ""12"", ""13"", ""14"", ""15""]
      label: Version
      immutable: true
    - key: postgres_user
      type: text
      label: User
      defaultValue: postgres
    - key: postgres_password
      type: secret
      label: Password
      immutable: true
    - key: postgres_database
      type: text
      label: Database
      defaultValue: postgres
";
}