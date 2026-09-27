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

container:
    dockerImage: postgres:${{ inputs.postgres_version }}
    env:
        POSTGRES_USER: ${{ inputs.postgres_user }}
        POSTGRES_PASSWORD: ${{ inputs.postgres_password }}
        POSTGRES_DB: ${{ inputs.postgres_database }}
    commandArgs:
        - -c
        - max_connections=${{ inputs.postgres_version }}
    port: 5432
    volumes:
        - name: postgres_data
          mount: /var/lib/postgresql/data

outputs:
    - key: connection_string
      label: Connection String
      secret: true
      value: postgresql://${{ env.POSTGRES_USER }}:${{ env.POSTGRES_PASSWORD }}@${{ runtime.host }}:${{ container.port }}/${{ env.POSTGRES_DB }}
";
}